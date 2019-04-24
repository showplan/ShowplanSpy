using CommandLine;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ShowplanSpy.CommandLine
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Options
    {
        [Option('p', "port", Default = 11188, Required = false, HelpText = "Port that the Web UI runs on.")]
        public int Port { get; set; }

        [Option('a', "appName",
            Required = false,
            HelpText = "Connection string AppName to monitor")]
        public string AppName { get; set; }

        [Option('d', "database",
            Required = true,
            HelpText = "Database to monitor")]
        public string Database { get; set; }

        [Option('s', "server", Required = false, Default = ".", HelpText = "Server to connect to. Highly recommended to not monitor remote servers")]
        public string Server { get; set; }
        
        [Option('u', "userName", Required = false, HelpText = "Username to connect with if SQL Auth is needed.")]
        public string UserName { get; set; }

        [Option('p', "password", Required = false, HelpText = "Password to connect with if SQL Auth is needed.")]
        public string Password { get; set; }

        [Option('c', "cleanup", Required = false, HelpText = "Cleans up any existing extended event sessions that might be lingering on startup.")]
        public bool CleanUp { get; set; }
    }
}
