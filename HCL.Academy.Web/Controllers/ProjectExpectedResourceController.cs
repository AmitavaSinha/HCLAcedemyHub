using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Web.Mvc;

namespace HCL.Academy.Web.Controllers
{
    public class ProjectExpectedResourceController : Controller
    {
        // GET: ProjectExpectedResource
        public ActionResult Index()
        {
            return View();
        }

        // GET: ProjectExpectedResource/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ProjectExpectedResource/Create
        /// <summary>
        /// Sets the expected resources and their respective skills
        /// </summary>
        /// <param name="projectID"></param>
        /// <returns></returns>
        public ActionResult Create(int projectID)
        {
            ProjectResources prjRes = new ProjectResources();
            prjRes.projectId = projectID;
            prjRes.skillResources = new List<SkillResource>();
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                prjRes = dal.GetExpectedProjectResourceCountByProjectId(prjRes);
            }
            catch(Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ProjectExpectedResource, Manage", ex.Message, ex.StackTrace));
            }
            return View(prjRes);
        }

        // POST: ProjectExpectedResource/Create
        [HttpPost]
        public ActionResult Create(ProjectResources prjRes)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                dal.AddExpectedProjectResourceCountByProjectId(prjRes);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ProjectExpectedResource, Manage", ex.Message, ex.StackTrace));
            }
            return View(prjRes);
        }

        // GET: ProjectExpectedResource/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ProjectExpectedResource/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, System.Web.Mvc.FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch(Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ProjectExpectedResource, Edit", ex.Message, ex.StackTrace));

                return View();
            }
        }

        // GET: ProjectExpectedResource/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ProjectExpectedResource/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, System.Web.Mvc.FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch(Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ProjectExpectedResource, Delete", ex.Message, ex.StackTrace));

                return View();
            }
        }
    }
}
