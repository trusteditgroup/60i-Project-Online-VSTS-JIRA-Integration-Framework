using System;
using ProjectOnlineSystemConnector.DataModel.OData;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public class AssignmentInfo
    {
        public Guid ResourceUid { get; set; }
        public string UserKey { get; set; }
        public string ResourceName { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public bool IsResourceExists { get; set; }
        public bool IsAssignmentExists { get; set; }
        public ODataResource ODataResource { get; set; }
        public int SystemId { get; set; }
        public string IssueKey { get; set; }
        public string IssueId { get; set; }
        public string AuthorKey { get; set; }
    }
}