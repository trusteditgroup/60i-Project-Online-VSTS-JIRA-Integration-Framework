using System.Web.Mvc;

namespace ProjectOnlineSystemConnector.Web.Helpers
{
    public class AllowCorsAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
            //filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS");
            //filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Headers",
            //    "Content-Type,X-Requested-With,Authorization,Origin,Accept," +
            //    "Access-Control-Request-Method,Access-Control-Allow-Methods,Access-Control-Allow-Headers,Access-Control-Allow-Origin,Access-Control-Request-Headers");
            base.OnActionExecuting(filterContext);
        }
    }
}