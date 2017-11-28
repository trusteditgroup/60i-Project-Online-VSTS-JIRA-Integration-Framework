using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using ProjectOnlineSystemConnector.SignalR;

[assembly: OwinStartup(typeof(Startup))]

namespace ProjectOnlineSystemConnector.SignalR
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Add configuration code or hub wire up here if needed
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
}