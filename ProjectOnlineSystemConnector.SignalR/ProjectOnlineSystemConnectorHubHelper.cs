using Microsoft.AspNet.SignalR.Client;
using Microsoft.Owin.Hosting;
using ProjectOnlineSystemConnector.DataModel.Common;

namespace ProjectOnlineSystemConnector.SignalR
{
    public static class ProjectOnlineSystemConnectorHubHelper
    {
        private static IHubProxy projectOnlineSystemConnectorHubProxy;

        public static void StartHubConnection(string url)
        {
            WebApp.Start<Startup>(url);
            var hubConnection = new HubConnection(url);
            projectOnlineSystemConnectorHubProxy = hubConnection.CreateHubProxy(nameof(ProjectOnlineSystemConnectorHub));
            hubConnection.Start().Wait();
        }

        public static void SendLogMessage(LogMessage logMessage)
        {
            projectOnlineSystemConnectorHubProxy.Invoke("SendLogMessage", logMessage);
        }
    }
}