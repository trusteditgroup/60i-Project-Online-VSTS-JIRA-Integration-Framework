using System;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public class StagingDTO
    {
        public int StagingId { get; set; }
        public int SystemId { get; set; }
        public DateTime RecordDateCreated { get; set; }
        public DateTime RecordDateUpdated { get; set; }
        public string WebHookEvent { get; set; }
        public string RecordState { get; set; }
        public string ChangedFields { get; set; }
        public string ProjectId { get; set; }
        public string ProjectKey { get; set; }
        public string ProjectName { get; set; }
        public string IssueId { get; set; }
        public string IssueKey { get; set; }
        public string IssueTypeId { get; set; }
        public string IssueTypeName { get; set; }
        public bool IsSubTask { get; set; }
        public string IssueName { get; set; }
        public string ParentEpicId { get; set; }
        public string ParentEpicKey { get; set; }
        public string ParentSprintId { get; set; }
        public string ParentSprintName { get; set; }
        public string ParentVersionId { get; set; }
        public string ParentVersionName { get; set; }
        public bool? ParentVersionReleased { get; set; }
        public string ParentIssueId { get; set; }
        public string ParentIssueKey { get; set; }
        public DateTime? DateStart { get; set; }
        public DateTime? DateFinish { get; set; }
        public string Assignee { get; set; }
        public string IssueStatus { get; set; }
        public DateTime? DateRelease { get; set; }
        public DateTime? DateStartActual { get; set; }
        public DateTime? DateFinishActual { get; set; }
        public int? IssueActualWork { get; set; }
        public int? OriginalEstimate { get; set; }
        public string RecordStateActual { get; set; }
        public string RecordStateGeneral { get; set; }
    }
}