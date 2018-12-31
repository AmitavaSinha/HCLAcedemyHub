using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace HCL.Academy.Web.Controllers
{
    public class ProjectController : Controller
    {
        // GET: Project
        public PartialViewResult Index()
        {
            List<Project> lstProj = new List<Project>();
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                lstProj = dal.GetAllProjects();       //Gets all projects
                
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "OnBoard, GetInformation", ex.Message, ex.StackTrace));
            }
            return PartialView(lstProj);
        }
        /// <summary>
        /// fetches a list of all projects
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [HttpGet]
        public ActionResult AddProject()
        {
            Project addProject = new Project();
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                List <Project> lstAllProjects = dal.GetAllProjects();
                addProject.ProjectName = String.Empty;
                Session["Projects"] = lstAllProjects;
                
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "OnBoard, GetInformation", ex.Message, ex.StackTrace));
            }
            return View(addProject);
        }
        /// <summary>
        /// Adds a new Project to the list of projects which is the input to this action.
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [HttpPost]

        public ActionResult AddProject(string projectName)
        {
            IDAL dal = (new DALFactory()).GetInstance();

            if (projectName.Equals(String.Empty))
            {
                ModelState.AddModelError("ProjectName", "Project Name is required");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    dal.AddProject(projectName);
                    List<Project> lstProjects = dal.GetAllProjects();
                    if (lstProjects.Count != 0)
                    {
                        Session["Projects"] = lstProjects;
                    }
                    return RedirectToAction("AddProject");
                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Project, AddProject", ex.Message, ex.StackTrace));

                return View();
            }
        }
        /// <summary>
        /// Edit the information of the selcted project
        /// </summary>
        /// <param name="projectID"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [SessionExpire]
        public ActionResult EditProjects(int projectID)
        {
            Project project = new Project();
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                project = dal.EditProjectByID(projectID);   //Fetches the details selected project

                Session["EditProject"] = project;
                
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "OnBoard, GetInformation", ex.Message, ex.StackTrace));

            }

            return View(project);
        }
        /// <summary>
        /// Edits/Updates the information of the selected project.
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public ActionResult EditProjects(string projectName)
        {
            if (projectName.Equals(String.Empty))
            {
                ModelState.AddModelError("ProjectName", "Project Name is required");
            }
            try
            {
                if (ModelState.IsValid)
                {
                    Project project = (Project)Session["EditProject"];
                    project.ProjectName = projectName;

                    IDAL dal = (new DALFactory()).GetInstance();
                    dal.UpdateProject(project);

                    return RedirectToAction("AddProject", new Project());
                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Project, EditProjects", ex.Message, ex.StackTrace));

                return View();
            }
        }
        /// <summary>
        /// Delete selected project
        /// </summary>
        /// <param name="projectID"></param>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        public ActionResult DeleteProject(int projectID)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                dal.RemoveProject(projectID);
                return RedirectToAction("AddProject", new Project());
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Project, DeleteProject", ex.Message, ex.StackTrace));

                return PartialView(new Project());
            }
        }


        public ActionResult ManageSkills(int projectID)
        {
            return View();
        }

    }
}