using System.Web.Http.Cors;

namespace ProjectOnlineSystemConnector.Web.Controllers
{
    [EnableCors("*", "*", "*")]
    public class CommonController : BaseController
    {
    }
}