using System.Collections.Generic;

namespace ProjectOnlineSystemConnector.DataModel.DTO
{
    public class AssignmentInfoAuthorKeyComparer : IEqualityComparer<AssignmentInfo>
    {
        public bool Equals(AssignmentInfo x, AssignmentInfo y)
        {
            return x.SystemId == y.SystemId && x.AuthorKey == y.AuthorKey;
        }

        public int GetHashCode(AssignmentInfo obj)
        {
            return base.GetHashCode();
        }
    }
}