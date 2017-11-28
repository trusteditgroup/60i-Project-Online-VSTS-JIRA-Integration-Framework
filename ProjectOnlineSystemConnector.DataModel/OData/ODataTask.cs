using System;

namespace ProjectOnlineSystemConnector.DataModel.OData
{
    public class ODataTask
    {
        public Guid ProjectId { get; set; }
        public Guid TaskId { get; set; }
        public Guid ParentTaskId { get; set; }
        public string TaskName { get; set; }
        public int SystemId { get; set; }
        public string IssueId { get; set; }
        public string IssueKey { get; set; }
        public string ParentEpicKey { get; set; }
        public string ParentVersionId { get; set; }
        public string IssueTypeName { get; set; }
    }
}