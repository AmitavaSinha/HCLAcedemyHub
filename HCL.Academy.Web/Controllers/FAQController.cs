using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.UI;

namespace HCLAcademy.Controllers
{
    public class FAQController : Controller
    {
        /// <summary>
        /// Get the FAQs page
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByCustom = "User", VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public ActionResult FAQ()
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                List<WikiPolicies> wikiPol = dal.GetAllWikiPolicies();      //Get all the Wiki policies
                WikiPolicyDocuments poldocs = new WikiPolicyDocuments();
                poldocs.ListOfWiki = wikiPol;
                return View(poldocs);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "FAQ, FAQ", ex.Message, ex.StackTrace));

                throw;
            }
        }
    }
}