using HCL.Academy.Web.DAL;
using HCL.Academy.Web.Models;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HCL.Academy.Web.Controllers
{
    public class ErrorLogController : Controller
    {
        private readonly AzureStorageTableOperations tblOps = new AzureStorageTableOperations();

        // GET: /<controller>/
        public ActionResult Index()
        {
            try
            {
                List<ErrorLogEntity> entities = tblOps.GetEntities<ErrorLogEntity>(AppConstant.PartitionError);
                Session[AppConstant.ErrorLogEntities] = entities;
                return View(entities);
            }
            catch (System.Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ErrorLog, Index", ex.Message, ex.StackTrace));
                return View();
            }
        }

        public ActionResult Create()
        {
            if (ModelState.IsValid)
            {
                var entity = new ErrorLogEntity();
                return View(entity);
                //return View();
            }
            else
            {
                return View();
            }
        }

        [HttpPost]
        public ActionResult Create(ErrorLogEntity entity)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    tblOps.AddEntity(entity, AppConstant.PartitionError);
                    //LogHelper.AddLog(new LogEntity(Constants.PARTITION_INFORMATIONLOG, HttpContext.User.Identity.Name.ToString(), ApplicationModules.CACHING, "New Configuration added for RedisCache"));
                    return RedirectToAction("Index");
                }
                else
                {
                    return View();
                }
            }
            catch (System.Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ErrorLog, Create", ex.Message, ex.StackTrace));
                return View();

            }

        }

        public ActionResult Edit(string Id)
        {
            try
            {
                IEnumerable<ErrorLogEntity> entities = Session[AppConstant.ErrorLogEntities] as IEnumerable<ErrorLogEntity>;
                if (entities != null)
                {
                    var entity = entities.Where(s => s.RowKey == Id).FirstOrDefault();
                    return View(entity);
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            catch (System.Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ErrorLog, Edit", ex.Message, ex.StackTrace));
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public ActionResult Edit(ErrorLogEntity entity)
        {
            try
            {
                tblOps.EditEntity(entity);
            }
            catch (System.Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ErrorLog, Edit", ex.Message, ex.StackTrace));

            }

            return RedirectToAction("Index");
        }

        public ActionResult Delete(string Id)
        {
            try
            {
                IEnumerable<ErrorLogEntity> entities = Session[AppConstant.ErrorLogEntities] as IEnumerable<ErrorLogEntity>;
                var entity = entities.Where(s => s.RowKey == Id).FirstOrDefault();
                tblOps.DeleteEntity(entity);
                return RedirectToAction("Index");
            }
            catch (System.Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ErrorLog, Delete", ex.Message, ex.StackTrace));
                return View();
            }
        }
    }
}