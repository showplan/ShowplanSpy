using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using ShowplanSpy.Hubs;

namespace ShowplanSpy.Web
{
    internal class Startup
    {
        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCors("CorsPolicy");
            app.UseSignalR(routes =>
            {
                routes.MapHub<ShowplanHub>("/showPlanHub");
            });
        }
    }
}