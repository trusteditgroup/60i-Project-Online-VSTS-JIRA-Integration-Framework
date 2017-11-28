using System.Web.Http.Cors;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.Web.Controllers
{
    [EnableCors("*", "*", "*")]
    public class SyncSystemFieldMappingCRUDController : BaseCRUDController<SyncSystemFieldMapping, SyncSystemFieldMappingDTO>
    {
        public SyncSystemFieldMappingCRUDController(SyncSystemFieldMappingBusinessService referenceBusinessService) : base(
            referenceBusinessService)
        {
        }
    }
}