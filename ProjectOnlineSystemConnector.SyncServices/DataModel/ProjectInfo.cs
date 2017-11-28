using System;
using Microsoft.ProjectServer.Client;
using ProjectOnlineSystemConnector.DataAccess.CSOM;

namespace ProjectOnlineSystemConnector.SyncServices.DataModel
{
    public class ProjectInfo
    {
        public Guid ProjectGuid { get; set; }
        public PublishedProject PublishedProject { get; set; }
        public DraftProject DraftProject { get; set; }
        public ProjectOnlineAccessService ProjectOnlineAccessService { get; set; }
    }
}