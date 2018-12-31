using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Web.Mvc;

namespace HCL.Academy.Web.Controllers
{
    public class AssignmentController : Controller
    {
        // GET: Assignment
        public ActionResult Index()
        {
            AssignUser assignUser = new AssignUser();
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                assignUser.lstProjects = dal.GetAllProjects();      //Get a list of all projects
                assignUser.lstUsers = dal.GetUsers();
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Assignment, Index", ex.Message, ex.StackTrace));

            }
            return View(assignUser);
        }
        /// <summary>
        /// Assign a user to the selected project
        /// </summary>
        /// <param name="assignUser"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public ActionResult Index(AssignUser assignUser)
        {
            AssignUser newUser = new AssignUser();

            IDAL dal = (new DALFactory()).GetInstance();

            try
            {
                if (assignUser.selectedUser == null)        //Checking whether a user is selected
                {
                    ModelState.AddModelError("selectedUser", "Please select an Employee");
                    newUser.lstProjects = dal.GetAllProjects();
                    newUser.lstUsers = dal.GetUsers();
                    newUser.selectedProject = assignUser.selectedProject;
                    return View(newUser);
                }
                if (assignUser.selectedProject == null)     //Checking whether a project is selected
                {
                    ModelState.AddModelError("selectedProject", "Please select a Project");
                    newUser.lstProjects = dal.GetAllProjects();
                    newUser.lstUsers = dal.GetUsers();
                    newUser.selectedUser = assignUser.selectedUser;
                    return View(newUser);
                }

                if (assignUser.selectedProject == null && assignUser.selectedUser == null)      //Checking whether a user and project are selected or not
                {
                    ModelState.AddModelError("selectedUser", "Please select an Employee");
                    ModelState.AddModelError("selectedProject", "Please select a Project");
                    assignUser.lstProjects = dal.GetAllProjects();
                    assignUser.lstUsers = dal.GetUsers();
                    return View(assignUser);
                }

                if (ModelState.IsValid)
                {
                    dal.UpdateProjectData(assignUser);          //Assign the user to the selected project
                }

                return RedirectToAction("Index", "Assignment");
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Assignment, Index", ex.Message, ex.StackTrace));

                return View(newUser);
            }
        }

    }
}