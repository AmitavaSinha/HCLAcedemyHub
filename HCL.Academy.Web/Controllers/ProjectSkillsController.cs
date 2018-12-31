using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Web.Mvc;

namespace HCLAcademy.Controllers
{
    public class ProjectSkillsController : Controller
    {
        [Authorize]
        [SessionExpire]
        public ActionResult Manage(int projectid)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                UserOnBoarding objOnBoarding = new UserOnBoarding();
                objOnBoarding.Skills = dal.GetAllSkills();      //Fetches all skills
                return View("ProjectSkills", objOnBoarding);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ProjectSkills, Manage", ex.Message, ex.StackTrace));
                return View();
            }
        }

        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult GetProjectSkills(string projectid)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            ProjectDetails objProjectDetails = dal.GetProjectSkillsByProjectID(projectid);      //Gets a list of Skills associated with the selected project.
            return new JsonResult { Data = objProjectDetails };
        }
        /// <summary>
        /// Update skills related to a Project
        /// </summary>
        /// <param name="projectid"></param>
        /// <param name="skillid"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult PostProjectSkill(string projectid, string skillid)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                ProjectSkill objProjectSkill = dal.PostProjectSkill(projectid, skillid);
                return new JsonResult { Data = objProjectSkill };
            }            
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ProjectSkills, DeleteProjectSkill", ex.Message, ex.StackTrace));

                return new JsonResult { Data = null };
            }
        }        
        /// <summary>
        /// Delete the selected Skill associated to a project.
        /// </summary>
        /// <param name="projectskillid"></param>
        /// <param name="projectid"></param>
        /// <param name="skillid"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult DeleteProjectSkill(int projectskillid, string projectid, string skillid)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                dal.DeleteProjectSkill(projectskillid, projectid, skillid);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ProjectSkill, DeleteProjectSkill", ex.Message, ex.StackTrace));

                return new JsonResult { Data = false };
            }
            return new JsonResult { Data = true };
        }

        

        
    }
}