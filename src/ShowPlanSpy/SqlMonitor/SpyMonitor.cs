using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlServer.XEvent;
using Microsoft.SqlServer.XEvent.Linq;
using Serilog;

namespace ShowplanSpy.SqlMonitor
{
    internal static class SpyMonitor
    {
        private const string XePlanNamePrefix = "ShowplanSpy";
        
        public class SpyOptions
        {
            public string Database { get; set; }
            public string Server { get; set; }
            public string AppName { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
            public bool CleanOnStart { get; set; }
        }

        public static async Task Spy(Action<ShowplanEvent> eventFunc, SpyOptions options, CancellationToken token)
        {
            await Task.Run(async () =>
            {
                var sessionName = $"{XePlanNamePrefix}-{Environment.MachineName}-{Guid.NewGuid()}";
                var connectionString = ConnectionStringBuilder.Build(
                    "master",
                    options.Server,
                    options.UserName,
                    options.Password);


                var store = new XEStore(new SqlStoreConnection(new SqlConnection(connectionString)));

                if (options.CleanOnStart)
                {
                    CleanUpExistingSessions(store);
                }

                var existingCount = store.Sessions.Count(s => s.Name.StartsWith(XePlanNamePrefix));
                if (existingCount > 0)
                {
                    Log.Logger.Warning("Found {count} existing plans that might need cleanup. Run with -c option to remove them.", existingCount);
                }
                
                var filterExpression = FilterBuilder.Build(options.Database, options.AppName);

                var session = store.CreateSession(sessionName);
                session.MaxDispatchLatency = 1;
                session.AutoStart = false;

                var showPlanEvent = session.AddEvent("sqlserver.query_post_execution_showplan");
                showPlanEvent.PredicateExpression = filterExpression;
                showPlanEvent.AddAction("sqlserver.sql_text");
                showPlanEvent.AddAction("sqlserver.query_hash");
                showPlanEvent.AddAction("sqlserver.query_plan_hash");
                showPlanEvent.AddAction("sqlserver.plan_handle");

                try
                {
                    Log.Logger.Verbose("Creating new session {session}", session.Name);
                    session.Create();
                    Log.Logger.Verbose("Starting new session {session}", session.Name);
                    session.Start();
                }
                catch (Exception e)
                {
                    Log.Logger.Fatal("Unable to create monitoring session", e);
                    throw;
                }

                Log.Logger.Verbose("Session {session} started", session.Name);

                // if the task gets canceled then we need to break out of the loop and clean up the session
                // we're checking in the loop for the cancellation but that will only hit if an event is triggered
                // so this lets us be a bit more aggressive about quitting and cleaning up after ourselves
                token.Register(() => SessionCleanup(session));

                try
                {
                    using (var eventStream = new QueryableXEventData(connectionString, sessionName,
                        EventStreamSourceOptions.EventStream, EventStreamCacheOptions.DoNotCache))
                    {
                        Log.Logger.Verbose("Watching new session");

                        foreach (var evt in eventStream)
                        {
                            if (token.IsCancellationRequested)
                            {
                                Log.Logger.Verbose("Cancelling sql spy task");
                                break;
                            }

                            try
                            {
                                var sqlEvent = new ShowplanEvent
                                {
                                    ShowplanXml = (XMLData) evt.Fields["showplan_xml"].Value,
                                    Duration = (ulong) evt.Fields["duration"].Value,
                                    EstimatedCost = (int) evt.Fields["estimated_cost"].Value,
                                    EstimatedRows = (int) evt.Fields["estimated_rows"].Value,
                                    QueryHash = (ulong) evt.Actions["query_hash"].Value,
                                    QueryPlanHash = (ulong) evt.Actions["query_plan_hash"].Value,
                                    SqlStatement = (string) evt.Actions["sql_text"].Value,
                                    PlanHandle = (byte[]) evt.Actions["plan_handle"].Value
                                };
                                
                                eventFunc(sqlEvent);
                            }
                            catch (Exception e)
                            {
                                Log.Logger.Error("Error creating event", e);
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Log.Logger.Error("Unknown error while monitoring events", e);
                        throw;
                    }
                }

                await Task.Delay(0, token);
            }, token);            
        }

        private static void SessionCleanup(Session session)
        {
            try
            {
                Log.Logger.Verbose("Trying to stop session {session}", session.Name);
                session.Stop();
            }
            catch (Exception e)
            {
                Log.Logger.Warning("Could not stop session {session}", session.Name, e);
            }

            try
            {
                Log.Logger.Verbose("Trying to drop session {session}", session.Name);
                session.Drop();
            }
            catch (Exception e)
            {
                Log.Logger.Warning("Could not drop session {session}", session.Name, e);
            }
        }

        private static void CleanUpExistingSessions(BaseXEStore store)
        {
            Log.Logger.Information("Cleaning existing sessions");
            var sessionsToClean = store.Sessions.Where(i => i.Name.StartsWith(XePlanNamePrefix)).ToList();
            
            foreach (var ses in sessionsToClean)
            {
                Log.Logger.Verbose("Deleting session {session}.", ses.Name);
                SessionCleanup(ses);
            }
        }
    }
}
