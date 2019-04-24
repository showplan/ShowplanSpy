using System.Data.SqlClient;

namespace ShowplanSpy.SqlMonitor
{
    internal static class ConnectionStringBuilder
    {
        public static string Build(string database, string server, string userName = null, string password = null)
        {
            var csb = GetBaseConnectionStringBuild(database, server, userName, password);
            return csb.ConnectionString;
        }

        public static string BuildWithAppName(string database, string server, string applicationName, string userName = null, string password = null)
        {
            var csb = GetBaseConnectionStringBuild(database, server, userName, password);
            csb.ApplicationName = applicationName;

            return csb.ConnectionString;
        }

        private static SqlConnectionStringBuilder GetBaseConnectionStringBuild(string database, string server, string userName, string password)
        {
            var csb = new SqlConnectionStringBuilder {DataSource = server, InitialCatalog = database};

            if (userName != null && password != null)
            {
                csb.UserID = userName;
                csb.Password = password;
                csb.IntegratedSecurity = false;
            }
            else
            {
                csb.IntegratedSecurity = true;
            }

            return csb;
        }
    }
}
