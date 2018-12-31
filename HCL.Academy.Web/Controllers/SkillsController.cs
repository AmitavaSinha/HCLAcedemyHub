using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.UI;

namespace HCLAcademy.Controllers
{
    public class SkillsController : Controller
    {
        // GET: Skills
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByCustom = "User", VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public ActionResult Skills()
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                List<Skills> allSkills = dal.GetSkills();       //Gets details of all skills
                return View(allSkills);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ImportProject,UploadProjectDataFile", ex.Message, ex.StackTrace));
                return View();
            }
        }
    }
}