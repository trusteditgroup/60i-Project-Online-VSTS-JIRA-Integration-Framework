using System;
using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public class ProjectServerSystemLinkDTO
    {
        public int ProjectServerSystemLinkId { get; set; }
        public Guid ProjectUid { get; set; }
        public int SystemId { get; set; }
        public string ProjectKey { get; set; }
        public string ProjectName { get; set; }
        public string ProjectId { get; set; }
        public bool IsHomeProject { get; set; }
        public string EpicKey { get; set; }
        public string EpicName { get; set; }
        public string EpicId { get; set; }
        public DateTime? LastExecuted { get; set; }
        public string ExecuteStatus { get; set; }
        public DateTime DateCreated { get; set; }

        public List<ProjectServerSystemLinkDTO> Epics { get; set; }
        public String EpmProjectName { get; set; }
        public Int32 RowNumber { get; set; }
        public String SystemName { get; set; }
    }
}