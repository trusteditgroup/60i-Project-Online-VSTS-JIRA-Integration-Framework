using Microsoft.AspNet.SignalR;
using ProjectOnlineSystemConnector.DataModel.Common;

namespace ProjectOnlineSystemConnector.SignalR
{
    public class ProjectOnlineSystemConnectorHub : Hub
    {
        public void Subscribe(string projectUid)
        {
            Groups.Add(Context.ConnectionId, !string.IsNullOrEmpty(projectUid) ? projectUid : "Admin");
        }

        public void SendLogMessage(LogMessage logMessage)
        {
            if (logMessage.IsBroadcastMessage)
            {
                Clients.All.GetLogMessage(logMessage);
            }
            else
            {
                Clients.Group(logMessage.ProjectUid.ToString()).GetLogMessage(logMessage);
                Clients.Group("Admin").GetLogMessage(logMessage);
            }
        }
    }
}