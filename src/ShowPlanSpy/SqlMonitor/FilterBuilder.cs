namespace ShowplanSpy.SqlMonitor
{
    internal static class FilterBuilder
    {
        public static string Build(string databaseName, string appName = null)
        {
            var filterExpression = $"[sqlserver].[equal_i_sql_unicode_string]([sqlserver].[database_name],N'{databaseName}') AND [sqlserver].[client_app_name]<>N'ShowplanSpy'";
            if (appName != null)
            {
                filterExpression = $"({filterExpression} AND [sqlserver].[client_app_name]=N'{appName}')";
            }

            return filterExpression;
        }
    }
}
