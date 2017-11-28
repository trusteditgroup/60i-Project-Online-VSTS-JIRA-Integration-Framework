using System;
using System.Collections.Generic;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.DataModel.Common
{
    public class LinkEpmToSystemViewModel
    {
        public LinkEpmToSystemViewModel()
        {
            ProjectServerSystemLinks = new List<ProjectServerSystemLinkDTO>();
        }

        public Guid ProjectUid { get; set; }
        public int SystemId { get; set; }
        public List<ProjectServerSystemLinkDTO> ProjectServerSystemLinks { get; set; }
    }
}