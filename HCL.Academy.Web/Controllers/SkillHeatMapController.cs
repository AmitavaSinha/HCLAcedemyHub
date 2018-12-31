using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Web.Mvc;

namespace HCL.Academy.Web.Controllers
{
    public class SkillHeatMapController : Controller
    {
      
        // GET: SkillHeatMap/Details/5
        /// <summary>
        /// Fetch the project Details of the selected project ID
        /// </summary>
        /// <param name="projectID"></param>
        /// <returns></returns>
        public ActionResult Details(int projectID)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                HeatMapProjectDetail heatMapProjectDetail = dal.GetHeatMapProjectDetailByProjectID(projectID);
                return View(heatMapProjectDetail);
            }
            catch(Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SkillHeatMap,Details", ex.Message, ex.StackTrace));
                return View();
            }
        }
        /// <summary>
        /// Displays the Project Details
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Details(HeatMapProjectDetail p)
        {  
            return View(p);
        }
    }
}
