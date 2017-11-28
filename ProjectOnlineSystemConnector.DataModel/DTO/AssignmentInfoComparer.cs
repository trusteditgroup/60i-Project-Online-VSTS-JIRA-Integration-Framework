using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public class AssignmentInfoComparer : IEqualityComparer<AssignmentInfo>
    {
        public bool Equals(AssignmentInfo x, AssignmentInfo y)
        {
            return y != null && x != null
                   && x.SystemId == y.SystemId
                   && x.IssueKey == y.IssueKey
                   && x.IssueId == y.IssueId
                   && x.AuthorKey == y.AuthorKey;
        }

        public int GetHashCode(AssignmentInfo obj)
        {
            return base.GetHashCode();
        }
    }
}