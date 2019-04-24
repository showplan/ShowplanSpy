using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Caching;
using System.Xml;
using JetBrains.Annotations;
using Serilog;

namespace ShowplanSpy.SqlMonitor
{
    internal static class ShowplanFixer
    {
        private static readonly MemoryCache Cache = MemoryCache.Default;

        public static bool TryFixByHandle(SpyMonitor.SpyOptions options, ShowplanEvent showplanEvent)
        {
            var planByHandle = GetPlanByHandle(options, showplanEvent.PlanHandle);
            if (planByHandle == null)
            {
                return false;
            }


            showplanEvent.Showplan = ApplyBetterStatement(showplanEvent.ShowplanXml.RawString, planByHandle, out string statementText);
            showplanEvent.SqlStatement = statementText;
            return true;
        }

        [CanBeNull]
        private static string GetPlanByHandle(SpyMonitor.SpyOptions options, byte[] planHandle)
        {
            var handleHex = BitConverter.ToString(planHandle).Replace("-", "");
            Log.Logger.Verbose("Looking up better plan for query handle {handle}", handleHex);
            
            if (Cache[handleHex] is string cached)
            {
                Log.Logger.Verbose("Found query handle {handle} in cache", handleHex);
                return cached;
            }

            const string findByPlanHandle = "select query_plan from sys.dm_exec_query_plan(@handle)";

            var connString = ConnectionStringBuilder.BuildWithAppName(
                options.Database, 
                options.Server, 
                "ShowplanSpy", 
                options.UserName,
                options.Password);

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                using (var command = new SqlCommand(findByPlanHandle, conn))
                {
                    command.Parameters.AddWithValue("handle", planHandle);
                    var newPlan = command.ExecuteScalar();
                    if (!(newPlan is string showPlanXml))
                    {
                        return null;
                    }

                    Log.Logger.Verbose("Found better plan for {handle} in sys tables", handleHex);
                    Cache.Add(handleHex, showPlanXml, DateTimeOffset.Now.AddMinutes(5));
                    return showPlanXml;
                }
            }
        }

        public static string ApplyBetterStatement(string showplanWithRuntimeInfo, string showplanWithBetterStatement,
            out string statementText)
        {
            statementText = "";

            var betterStatementXml = new XmlDocument();
            betterStatementXml.LoadXml(showplanWithBetterStatement);

            var betterStatementValues = new Dictionary<string, Dictionary<string, string>>();
            var setStatements = new Dictionary<string, Dictionary<string, string>>();

            // build collection of better statement values. 
            var betterStatementXmlStatementsNodes = betterStatementXml.GetElementsByTagName("Statements");
            foreach (var statementsNodes in betterStatementXmlStatementsNodes.Cast<XmlNode>())
            {
                foreach (var statement in statementsNodes.ChildNodes.Cast<XmlNode>())
                {
                    if (statement.Attributes == null) continue;

                    var hash = statement.Attributes["QueryHash"].Value;
                    var attributes = statement.Attributes
                        .Cast<XmlAttribute>()
                        .ToDictionary(attribute => attribute.Name, attribute => attribute.Value);

                    betterStatementValues.Add(hash, attributes);

                    var setOptionNode = statement.ChildNodes
                        .Cast<XmlNode>()
                        .FirstOrDefault(i => i.Name == "StatementSetOptions");

                    if (setOptionNode?.Attributes != null)
                    {
                        var options = setOptionNode.Attributes
                            .Cast<XmlAttribute>()
                            .ToDictionary(attribute => attribute.Name, attribute => attribute.Value);

                        setStatements.Add(hash, options);
                    }
                }
            }

            var originalXml = new XmlDocument();
            originalXml.LoadXml(showplanWithRuntimeInfo);


            // build collection of better statement values. 
            var originalXmlStatementsNodes = originalXml.GetElementsByTagName("Statements");
            foreach (var statementsNodes in originalXmlStatementsNodes.Cast<XmlNode>())
            {
                foreach (var statement in statementsNodes.ChildNodes.Cast<XmlNode>())
                {
                    if (statement.Attributes == null) continue;

                    var hash = statement.Attributes["QueryHash"].Value;
                    var betterStatementAttributes = betterStatementValues[hash];

                    statementText = StripParamsFromSqlStatement(betterStatementAttributes["StatementText"].Trim());

                    foreach (var betterAttribute in betterStatementAttributes)
                    {
                        var statementAttribute = statement.Attributes[betterAttribute.Key];
                        if (statementAttribute == null)
                        {
                            statementAttribute = originalXml.CreateAttribute(betterAttribute.Key);
                            statement.Attributes.Append(statementAttribute);
                        }

                        statementAttribute.Value = statementAttribute.Name == "StatementText" ? statementText : betterAttribute.Value;
                    }

                    var setStatement = setStatements[hash];
                    if (setStatement != null)
                    {
                        var setOptionElement = originalXml.CreateElement("StatementSetOptions");
                        foreach (var setOption in setStatement)
                        {
                            var attribute = originalXml.CreateAttribute(setOption.Key);
                            attribute.Value = setOption.Value;

                            setOptionElement.Attributes.Append(attribute);
                        }

                        statement.AppendChild(setOptionElement);
                    }
                }
            }

            return originalXml.InnerXml;
        }

        public static string StripParamsFromSqlStatement(string original)
        {
            if (!original.StartsWith("(@"))
            {
                return original;
            }

            var firstClosingParam = original.IndexOf(")", StringComparison.Ordinal);
            return original.Substring(firstClosingParam + 1);
        }
    }
}
