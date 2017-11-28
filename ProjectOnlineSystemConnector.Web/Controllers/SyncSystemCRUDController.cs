using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http.Cors;
using System.Web.Mvc;
using Newtonsoft.Json;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Data.Views.EntityClasses;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.Web.Controllers
{
    [EnableCors("*", "*", "*")]
    public class SyncSystemCRUDController : BaseCRUDController<SyncSystem, SyncSystemDTO>
    {
        public SyncSystemCRUDController(SyncSystemBusinessService referenceBusinessService) : base(
            referenceBusinessService)
        {
        }

        public async Task<JsonResult> GetSyncSystemListAsync()
        {
            try
            {
                List<SyncSystemDTO> result = await SyncSystemBusinessService.GetSyncSystemListAsync();
                return Json(new ProxyResponse
                {
                    Data = JsonConvert.SerializeObject(result),
                    Result = "ok",
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                return HandleException(exception);
            }
        }
    }
}