using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace HCLAcademy.Controllers
{
    public class AssessmentController : Controller
    {
        private static int _assessmentId { get; set; }

        // GET: Assessment
        [Authorize]
        [SessionExpire]
        public ActionResult Index(int id)
        {
            _assessmentId = id;
            TempData["assmentID"] = _assessmentId;
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                var objAssessmentDetails = dal.GetAssessmentDetails(_assessmentId);         //Get assessment details based on Assessment ID

                if (!objAssessmentDetails.AssessmentCompletionStatus && !objAssessmentDetails.MaxAttemptsExceeded)
                {
                    Session["StartAssessment"] = objAssessmentDetails;
                }
                else
                {
                    return RedirectToAction("Home", "Home");
                }
                
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Assessment, Index", ex.Message, ex.StackTrace));

            }
            return View("StartAssessment");
        }
        /// <summary>
        /// Get assessment questions
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult AssessmentQuestions()
        {
            Assessments assessments = (Assessments)Session["StartAssessment"];
            return Json(assessments, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// Get the result of the assessment using the details of the question 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="QuestionDetails"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AssessmentResult(AssesmentResult result, List<QuestionDetail> QuestionDetails)
        {
            bool response = false;
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                response = dal.AssessmentResult(result, QuestionDetails);

                Response.RemoveOutputCacheItem("/onboard/onboarding");
                Response.RemoveOutputCacheItem("/home/getlearningjourney");
                Response.RemoveOutputCacheItem("/home/getassessments");
                Response.RemoveOutputCacheItem("/training/training");
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Assessment, AssessmentResult", ex.Message, ex.StackTrace));
                Utilities.LogToEventVwr(ex.StackTrace, 0);
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
    }
}