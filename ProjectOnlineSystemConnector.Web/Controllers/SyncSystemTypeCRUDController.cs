using System.Web.Http.Cors;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.Web.Controllers
{
    [EnableCors("*", "*", "*")]
    public class SyncSystemTypeCRUDController : BaseCRUDController<SyncSystemType, SyncSystemTypeDTO>
    {
        public SyncSystemTypeCRUDController(SyncSystemTypeBusinessService referenceBusinessService)
            : base(referenceBusinessService)
        {
        }
    }
}