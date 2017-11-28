using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataAccess.Database.UnitOfWork;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.BusinessServices
{
    public class SyncSystemSettingBusinessService : BaseBusinessService<SyncSystemSetting, SyncSystemSettingDTO>
    {
        public SyncSystemSettingBusinessService(UnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}