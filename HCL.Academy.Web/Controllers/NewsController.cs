using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Web.Mvc;
using System.Web.UI;


namespace HCLAcademy.Controllers
{
    public class NewsController : Controller
    {
        // GET: News
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByCustom = "User", VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]

        public ActionResult NewsEvents()
        {
            try
            {
                //  IDAL dal = (new DALFactory()).GetInstance();
                SharePointDAL dal = new SharePointDAL();
                string noImagePath = Server.MapPath(Url.Content("~/Images/noimage.png"));
                ViewBag.annclst = dal.GetNews(noImagePath);
                ViewBag.EventsList = dal.GetEvents();
                ViewBag.RssFeedVB = dal.GetRSSFeeds();     
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "News, NewsEvents", ex.Message, ex.StackTrace));
            }
            return View();
        }

    }
}