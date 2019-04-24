using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ShowplanSpy.Hubs;

namespace ShowplanSpy.Web
{
    public static class Host
    {
        public static IHubContext<ShowplanHub, IShowplanClient> ShowplanHub;

        public static async Task Run(int port, CancellationToken token)
        {
            var builder = WebHost.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    services.AddCors(options => options.AddPolicy("CorsPolicy", policy =>
                    {
                        policy
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            // this is the port vue server will launch the app if we are working the web frontend
                            .WithOrigins("http://localhost:8080", $"http://localhost:{port}") 
                            .AllowCredentials();
                    }));
                    services.AddSignalR();
                });

            var host = builder
                .UseKestrel(options => options.ListenLocalhost(port))
                .UseSerilog()
                .UseStartup<Startup>()                
                .Build();

            using (var serviceScope = host.Services.CreateScope())
            {
               ShowplanHub = serviceScope.ServiceProvider.GetRequiredService<IHubContext<ShowplanHub, IShowplanClient>>();
            }

            await host.RunAsync(token: token);
        }
    }
}
