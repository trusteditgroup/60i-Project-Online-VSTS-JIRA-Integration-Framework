using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.BusinessServices
{
    public class SyncSystemSettingValueBusinessService : BaseBusinessService<SyncSystemSettingValue, SyncSystemSettingValueDTO>
    {
        public SyncSystemSettingValueBusinessService(UnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}