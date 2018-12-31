using HCLAcademy.Models;
using System.Configuration;
using System.Web;
using System.Web.Mvc;

namespace HCLAcademy.CustomFilters
{
    public class CustomAuthorizationFilter : AuthorizeAttribute
    {
        //private readonly string AuthorizedGroup;

        string spOwnerGroup = ConfigurationManager.AppSettings["AcademyOwnerGroup"].ToString();
        string spPMOGroup = ConfigurationManager.AppSettings["AcademyPMO"].ToString();

        public CustomAuthorizationFilter()
        {
            //this.AuthorizedGroup = ConfigurationManager.AppSettings["AcademyPMO"].ToString();
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            bool authorize = false;
            if (HttpContext.Current.Session["CurrentUser"] != null)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                authorize = user.Groups.Exists(x => x == this.spPMOGroup);
                if (!authorize)
                {
                    authorize = user.Groups.Exists(x => x == this.spOwnerGroup);
                }
            }
            else
            {
                return authorize;
            }
            return authorize;
        }
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("~/Home/UnAuthorized/?groupName=Admin");

            ////if (this.AuthorizedGroup == spPMOGroup)
            ////{
            ////    filterContext.Result = new RedirectResult("~/Home/UnAuthorized/?groupName=Admin");
            ////}
            ////else
            ////{
            ////    filterContext.Result = new RedirectResult("~/Home/UnAuthorized/");
            ////}
        }
    }
}
