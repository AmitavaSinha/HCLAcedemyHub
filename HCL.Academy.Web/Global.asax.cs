using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace HCLAcademy
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            BundleTable.EnableOptimizations = false;
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            // event is raised each time a new session is created     
        }

        protected void Session_End(object sender, EventArgs e)
        {
           // Utilities.LogToEventVwr("Session expired", 0); No Need to Log Session Expiry - SKB
        }

        public override string GetVaryByCustomString(HttpContext context, string custom)
        {
            if (custom == "User")
            {
                // depends on your authentication mechanism
                return "User=" + context.User.Identity.Name;
                //?return "User=" + context.Session.SessionID;
            }
           // return base.GetVaryByCustomString(context, custom);
            return null;
        }
    }
}
