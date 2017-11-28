using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http.Cors;
using System.Web.Mvc;
using Newtonsoft.Json;
using ProjectOnlineSystemConnector.DataModel.Common;
using ProjectOnlineSystemConnector.DataModel.DTO;

namespace ProjectOnlineSystemConnector.Web.Controllers
{
    [EnableCors("*", "*", "*")]
    public class ProjectServerSystemLinkCRUDController : BaseController
    {
        public async Task<JsonResult> GetProjectServerSystemLinks(Guid? projectUid)
        {
            List<ProjectServerSystemLinkDTO> projectLinks = await ProjectServerSystemLinkBusinessService
                .GetLinksAndBlocksWithEpmProjectsAsync(projectUid);

            if (projectUid != null)
            {
                var linked = new LinkEpmToSystemViewModel
                {
                    ProjectUid = projectUid.Value,
                    ProjectServerSystemLinks = projectLinks.Where(x => x.ProjectUid == projectUid.Value).ToList()
                };
                var toBlock = new LinkEpmToSystemViewModel
                {
                    ProjectUid = projectUid.Value,
                    ProjectServerSystemLinks = projectLinks.Where(x => x.ProjectUid != projectUid.Value).ToList()
                };

                return Json(new ProxyResponse
                {
                    Result = "ok",
                    Data = JsonConvert.SerializeObject(new
                    {
                        Linked = linked,
                        ToBlock = toBlock
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(new ProxyResponse
            {
                Result = "ok",
                Data = JsonConvert.SerializeObject(new
                {
                    Linked = new LinkEpmToSystemViewModel
                    {
                        ProjectServerSystemLinks = projectLinks
                    }
                })
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<JsonResult> LinkEpmToSystem(ProjectServerSystemLinkDTO syncObjectLinkViewModel)
        {
            {
                try
                {
                    await ProjectServerSystemLinkBusinessService.LinkEpmToSystem(syncObjectLinkViewModel);
                }
                catch (Exception exception)
                {
                    JsonResult result = HandleException(exception);
                    return result;
                }
            }
            return Json(new ProxyResponse
            {
                Result = "ok",
            }, JsonRequestBehavior.AllowGet);
        }
    }
}