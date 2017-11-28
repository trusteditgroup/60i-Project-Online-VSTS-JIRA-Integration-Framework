using System.Data.Entity;
using System.Linq;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.Repository.Base;

namespace ProjectOnlineSystemConnector.DataAccess.Database.Repository.Entity
{
    public class StagingRepository : GenericRepository<Staging>
    {
        public StagingRepository(DbContext context) : base(context)
        {
        }

        public Staging GetIssueById(int systemId, string issueId, string issueTypeId)
        {
            return DbSet.OrderByDescending(x => x.RecordDateUpdated).FirstOrDefault(x => x.SystemId == systemId && x.IssueId == issueId && x.IssueTypeId == issueTypeId);
        }
    }
}