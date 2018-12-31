using HCL.Academy.Web.DAL;
using HCLAcademy.Models;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.UI;

namespace HCLAcademy.Controllers
{
    [SessionState(System.Web.SessionState.SessionStateBehavior.ReadOnly)]
    [OutputCache(Duration = 600, VaryByParam = "none", Location = OutputCacheLocation.Client, NoStore = false)]
    public class BaseController : Controller
    {
        /// <summary>
        /// Get the Menu/Navigation bar of the website
        /// </summary>
        public void GetMenu()
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<SiteMenu> siteMenu = dal.GetMenu();
            System.Web.HttpContext.Current.Session["UserSiteMenu"] = siteMenu;
        }
    }
}