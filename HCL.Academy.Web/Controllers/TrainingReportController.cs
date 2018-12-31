using HCL.Academy.Web.DAL;
using HCLAcademy.Models;
using System.Collections.Generic;
using System.Web.Mvc;

namespace HCLAcademy.Controllers
{
    public class TrainingReportController : Controller
    {
        [Authorize]
        [SessionExpire]
        public ActionResult TrainingReport()
        {
            UserOnBoarding objUserOnBoarding = new UserOnBoarding();
            IDAL dal = (new DALFactory()).GetInstance();
            objUserOnBoarding.Skills = dal.GetAllSkills();          //Gets a list of all skills for Selection
            return View(objUserOnBoarding);
        }
        /// <summary>
        /// Fetches the related Training Report for the selected Skill and Competency
        /// </summary>
        /// <param name="skillid"></param>
        /// <param name="competencyid"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult GetTrainingsReport(string skillid, string competencyid)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            TrainingReport usersList = dal.GetTrainingsReport(skillid, competencyid);
            return new JsonResult { Data = usersList };
        }        
    }
}