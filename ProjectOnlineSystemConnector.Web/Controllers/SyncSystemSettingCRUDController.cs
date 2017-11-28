using System.Web.Http.Cors;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.Web.Controllers
{
    [EnableCors("*", "*", "*")]
    public class SyncSystemSettingCRUDController : BaseCRUDController<SyncSystemSetting, SyncSystemSettingDTO>
    {
        public SyncSystemSettingCRUDController(SyncSystemSettingBusinessService referenceBusinessService) : base(
            referenceBusinessService)
        {
        }
    }
}