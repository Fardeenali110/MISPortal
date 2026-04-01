using System.Web;
using System.Web.Mvc;

namespace MisProject
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new AuthorizeAttribute()); // Global authorization for ALL controllers
        }
    }
}