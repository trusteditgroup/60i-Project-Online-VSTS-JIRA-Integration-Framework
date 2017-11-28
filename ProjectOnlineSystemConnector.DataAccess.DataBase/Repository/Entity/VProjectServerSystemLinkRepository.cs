using System.Data.Entity;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.Repository.Base;

namespace ProjectOnlineSystemConnector.DataAccess.Database.Repository.Entity
{
    public class VProjectServerSystemLinkRepository : GenericRepository<VProjectServerSystemLink>
    {
        public VProjectServerSystemLinkRepository(DbContext context) : base(context)
        {
        }
    }
}