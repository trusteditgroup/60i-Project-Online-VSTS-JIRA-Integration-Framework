using System.Web.Mvc;
using ProjectOnlineSystemConnector.Web.Helpers;

namespace ProjectOnlineSystemConnector.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new AllowCorsAttribute());
        }
    }
}