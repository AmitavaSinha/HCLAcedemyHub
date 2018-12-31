using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Web.Mvc;

namespace HCLAcademy.Controllers
{
    public class ProjectResourceController : Controller
    {
        [Authorize]
        [SessionExpire]
        public ActionResult ResourceDetails(int projectID)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                Resource prjRes = dal.GetResourceDetailsByProjectID(projectID);     //Get the resource details of all Projects
                return View(prjRes);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ProjectResource, ResourceDetails", ex.Message, ex.StackTrace));

                return View();
            }
        }
    }
}
