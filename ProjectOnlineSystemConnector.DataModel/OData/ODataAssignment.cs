using System;

namespace ProjectOnlineSystemConnector.DataModel.OData
{
    public class ODataAssignment
    {
        public Guid TaskId { get; set; }
        public Guid ResourceId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid AssignmentId { get; set; }
        public int SystemId { get; set; }
        public string IssueKey { get; set; }
        public string IssueId { get; set; }
        public string ParentIssueId { get; set; }
        public string ParentEpicId { get; set; }
        public string ParentEpicKey { get; set; }
        public string ParentVersionId { get; set; }
        public string IssueTypeName { get; set; }
        public string TaskName { get; set; }
    }
}