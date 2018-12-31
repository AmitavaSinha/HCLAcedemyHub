using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace HCLAcademy.Controllers
{
    public class SearchController : Controller
    {
        private static List<Result> lstResult;
        /// <summary>
        /// Fetches results for a particular Keyword
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [SessionExpire]
        public ActionResult Search(string keyword)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                lstResult = dal.Search(keyword);
                return RedirectToAction("Search", "Search");
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Search, Search", ex.Message, ex.StackTrace));
                return View();
            }
        }
        /// <summary>
        /// Search for a particular keyword
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        public ActionResult Search()
        {
            if (lstResult != null && lstResult.Count > 0)
            {
                ViewBag.lstResults = lstResult;
            }
            else
            {
                ViewBag.lstResults = null;
            }
            return View();
        }
    }
}