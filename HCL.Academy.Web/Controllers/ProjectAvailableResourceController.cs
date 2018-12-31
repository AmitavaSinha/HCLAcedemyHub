using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;

namespace HCL.Academy.Web.Controllers
{
    public class ProjectAvailableResourceController : Controller
    {
        // GET: ProjectAvailableResource
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Create(int projectID)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            ProjectResources prjRes = new ProjectResources();
            prjRes.projectId = projectID;
            List<SkillResource> lstSkillResource = new List<SkillResource>();
            try
            {
                List<ProjectSkillResource> projectSkillResources = dal.GetAllProjectSkillResourcesByProjectID(projectID);       //Get the resources and their skills assigned to the selcted Project.
                Hashtable objHashTable = new Hashtable();
                if (projectSkillResources != null && projectSkillResources.Count > 0)
                {
                    foreach (var item in projectSkillResources)
                    {
                        if (!objHashTable.ContainsKey(item.SkillId))
                        {
                            objHashTable.Add(item.SkillId, item.SkillId);
                            SkillResource objSkillResource = new SkillResource();
                            objSkillResource.skillId = item.SkillId;
                            objSkillResource.skill = item.Skill;
                            lstSkillResource.Add(objSkillResource);
                        }
                    }
                    prjRes.skillResources = lstSkillResource;

                    foreach (SkillResource skr in prjRes.skillResources)
                    {
                        foreach (var item in projectSkillResources)
                        {
                            if (skr.skillId == item.SkillId)
                            {
                                switch (item.CompetencyLevel.ToUpper())
                                {
                                    case "NOVICE":
                                        skr.beginnerCount = Convert.ToInt32(item.AvailableResourceCount == String.Empty ? "0" : item.AvailableResourceCount);
                                        break;
                                    case "ADVANCED BEGINNER":
                                        skr.advancedBeginnerCount = Convert.ToInt32(item.AvailableResourceCount == String.Empty ? "0" : item.AvailableResourceCount);
                                        break;

                                    case "COMPETENT":
                                        skr.competentCount = Convert.ToInt32(item.AvailableResourceCount == String.Empty ? "0" : item.AvailableResourceCount);
                                        break;

                                    case "PROFICIENT":
                                        skr.proficientCount = Convert.ToInt32(item.AvailableResourceCount == String.Empty ? "0" : item.AvailableResourceCount);
                                        break;

                                    case "EXPERT":
                                        skr.expertCount = Convert.ToInt32(item.AvailableResourceCount == String.Empty ? "0" : item.AvailableResourceCount);
                                        break;
                                    default:
                                        break;
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ProjectAvailableResource, Create", ex.Message, ex.StackTrace));
            }
            return View(prjRes);
        }

        // POST: ProjectExpectedResource/Create
        /// <summary>
        /// Adds/Updates the resources assigned to a Project
        /// </summary>
        /// <param name="prjRes"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Create(ProjectResources prjRes)
        {
            try
            {
                string url = ConfigurationManager.AppSettings["URL"].ToString();
                if (ModelState.IsValid)
                {
                    IDAL dal = (new DALFactory()).GetInstance();
                    dal.AddProjectSkillResources(prjRes);
                }
                ViewBag.Message = "Data updated";
                return View(prjRes);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ProjectAvailableResource, Create", ex.Message, ex.StackTrace));

                return View(prjRes);
            }
        }
    }
}