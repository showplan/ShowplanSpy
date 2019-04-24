using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Serilog;
using Serilog.Events;
using ShowplanSpy.CommandLine;
using ShowplanSpy.Hubs;
using ShowplanSpy.SqlMonitor;
using ShowplanSpy.Web;

namespace ShowplanSpy
{
    [UsedImplicitly]
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .WriteTo.Console()
                .CreateLogger();

            var result = Parser.Default.ParseArguments<Options>(args);
            if (result is Parsed<Options> successResult)
            {
                RunOptionsAndReturnExitCode(successResult.Value);
            }
        }

        private static void RunOptionsAndReturnExitCode(Options opts)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var spyOptions = new SpyMonitor.SpyOptions()
            {
                Password = opts.Password,
                AppName = opts.AppName,
                Database = opts.Database,
                Server = opts.Server,
                UserName = opts.UserName,
                CleanOnStart = opts.CleanUp
            };

            var spyTask = SpyMonitor.Spy(async evt => await HandleSqlEvent(spyOptions, evt),
                spyOptions, cancellationTokenSource.Token);

            var hostTask = Host.Run(opts.Port, cancellationTokenSource.Token);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Log.Logger.Verbose("CTRL-C pressed, shutting down tasks");               
                cancellationTokenSource.Cancel();
                eventArgs.Cancel = true;
            };

            try
            {
                Task.WaitAll(spyTask, hostTask);
                Log.Logger.Verbose("Done");
            }
            catch (AggregateException e) when (e.InnerException is TaskCanceledException)
            {
                // expected with ctrl-c behavior
            }
        }

        private static async Task HandleSqlEvent(SpyMonitor.SpyOptions options, ShowplanEvent sqlEvent)
        {
            Log.Logger.Verbose("Publishing query plan {hash}", sqlEvent.QueryPlanHashString);

            var found = ShowplanFixer.TryFixByHandle(options, sqlEvent);
            if (found)
            {
                Log.Logger.Verbose("Found better plan in sys tables");
            }
            else
            {
                sqlEvent.Showplan = sqlEvent.ShowplanXml.RawString;
            }

            var message = new ShowplanMessage()
            {
                Duration = sqlEvent.Duration,
                EstimatedCost = sqlEvent.EstimatedCost,
                EstimatedRows = sqlEvent.EstimatedRows,
                QueryPlanHashString = sqlEvent.QueryPlanHashString,
                QueryPlanHandle = BitConverter.ToString(sqlEvent.PlanHandle).Replace("-", ""),
                Showplan = sqlEvent.Showplan,
                OccuredAt = DateTimeOffset.Now,
                SqlStatement = sqlEvent.SqlStatement,
                GoodPlan = found
            };

            await Host.ShowplanHub.Clients.All.ReceivePlan(message);
        }
    }
}
