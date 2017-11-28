using System.Web.Http.Cors;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.Web.Controllers
{
    [EnableCors("*", "*", "*")]
    public class SyncSystemSettingValueCRUDController : BaseCRUDController<SyncSystemSettingValue, SyncSystemSettingValueDTO>
    {
        public SyncSystemSettingValueCRUDController(SyncSystemSettingValueBusinessService referenceBusinessService) : base(
            referenceBusinessService)
        {
        }
    }
}