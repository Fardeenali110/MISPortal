using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Optimization;

namespace MisProject
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            // Register global filters
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);

            // Register routes
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Register bundles (if using)
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            // Custom authentication logic if needed
        }
    }
}