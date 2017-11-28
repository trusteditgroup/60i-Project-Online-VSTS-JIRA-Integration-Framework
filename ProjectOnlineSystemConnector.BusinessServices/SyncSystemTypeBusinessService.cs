using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.BusinessServices
{
    public class SyncSystemTypeBusinessService : BaseBusinessService<SyncSystemType, SyncSystemTypeDTO>
    {
        public SyncSystemTypeBusinessService(UnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}