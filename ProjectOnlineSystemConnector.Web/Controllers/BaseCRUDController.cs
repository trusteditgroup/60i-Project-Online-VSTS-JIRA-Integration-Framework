using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http.Cors;
using System.Web.Mvc;
using Newtonsoft.Json;
using ProjectOnlineSystemConnector.BusinessServices;
using ProjectOnlineSystemConnector.Common;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.Web.Controllers
{
    [EnableCors("*", "*", "*")]
    public class BaseCRUDController<TEntity, TDTO> : BaseController where TEntity : class where TDTO : IDtoId
    {
        protected BaseBusinessService<TEntity, TDTO> ReferenceBusinessService { get; }

        public BaseCRUDController(BaseBusinessService<TEntity, TDTO> referenceBusinessService)
        {
            ReferenceBusinessService = referenceBusinessService;
        }

        public virtual async Task<JsonResult> GetListAsync()
        {
            List<TDTO> list = await ReferenceBusinessService.GetListAsync();
            return Json(new ProxyResponse
            {
                Result = AjaxStatus.Good,
                TotalCount = list.Count,
                Data = JsonConvert.SerializeObject(list)
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public virtual async Task<JsonResult> UpdateListAsync(List<TDTO> updateList)
        {
            await ReferenceBusinessService.UpdateListAsync(updateList);
            return Json(new ProxyResponse
            {
                Result = AjaxStatus.Good,
                Data = JsonConvert.SerializeObject(updateList),
                TotalCount = updateList.Count
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public virtual async Task<JsonResult> InsertListAsync(List<TDTO> insertList)
        {
            await ReferenceBusinessService.InsertListAsync(insertList);
            return Json(new ProxyResponse
            {
                Result = AjaxStatus.Good,
                Data = JsonConvert.SerializeObject(insertList),
                TotalCount = insertList.Count
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public virtual async Task<JsonResult> RemoveListAsync(List<int> deleteList)
        {
            await ReferenceBusinessService.RemoveListAsync(deleteList);
            return Json(new ProxyResponse
            {
                Result = AjaxStatus.Good,
                Data = JsonConvert.SerializeObject(deleteList),
                TotalCount = deleteList.Count
            }, JsonRequestBehavior.AllowGet);
        }
    }
}