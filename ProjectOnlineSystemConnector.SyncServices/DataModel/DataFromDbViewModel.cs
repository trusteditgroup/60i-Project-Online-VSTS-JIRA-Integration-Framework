using System;
using System.Collections.Generic;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.SyncServices.DataModel
{
    public class DataFromDbViewModel
    {
        public DataFromDbViewModel()
        {
            ProjectInfos = new List<ProjectInfo>();
            StagingsAll = new List<StagingDTO>();
            CustomValuesAll = new List<VStagingFieldMappingValue>();
            MasterWorklogs = new List<MasterWorklog>();
            ProjectServerSystemLinks = new List<ProjectServerSystemLinkDTO>();
            SyncSystems = new List<SyncSystemDTO>();
            NewTaskChangesTracker = new Dictionary<Guid, Dictionary<int, List<TaskInfo>>>();
            UpdateTaskChangesTracker = new Dictionary<Guid, List<TaskInfo>>();
            NewProjectResourcesTracker = new Dictionary<Guid, List<AssignmentInfo>>();
            Assignments = new List<AssignmentInfo>();
        }

        public List<ProjectInfo> ProjectInfos { get; set; }
        public List<StagingDTO> StagingsAll { get; set; }
        public List<VStagingFieldMappingValue> CustomValuesAll { get; set; }
        public List<ProjectServerSystemLinkDTO> ProjectServerSystemLinks { get; set; }
        public List<SyncSystemDTO> SyncSystems { get; set; }
        public Dictionary<Guid, Dictionary<int, List<TaskInfo>>> NewTaskChangesTracker { get; set; }
        public Dictionary<Guid, List<TaskInfo>> UpdateTaskChangesTracker { get; set; }
        public Dictionary<Guid, List<AssignmentInfo>> NewProjectResourcesTracker { get; set; }
        public List<MasterWorklog> MasterWorklogs { get; set; }
        public List<AssignmentInfo> Assignments { get; set; }
        public List<Master> ParentIssues { get; set; }
    }
}