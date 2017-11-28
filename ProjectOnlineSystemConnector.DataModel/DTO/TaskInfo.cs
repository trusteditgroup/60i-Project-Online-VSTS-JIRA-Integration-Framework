using System;
using System.Collections.Generic;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public class TaskInfo
    {
        public TaskInfo()
        {
            AddedAssignments = new List<AssignmentInfo>();
            StagingCustomValues = new List<VStagingFieldMappingValue>();
            AffectedStagings = new List<StagingDTO>();
        }

        public Guid ProjectUid { get; set; }
        public StagingDTO MainStaging { get; set; }
        public List<StagingDTO> AffectedStagings { get; set; }
        public bool HasChanges { get; set; }
        public List<VStagingFieldMappingValue> StagingCustomValues { get; set; }
        public List<AssignmentInfo> AddedAssignments { get; set; }
        public string DefaultParentTaskName { get; set; }
        public bool IsHomeProject { get; set; }
        public Guid TaskUid { get; set; }
    }
}