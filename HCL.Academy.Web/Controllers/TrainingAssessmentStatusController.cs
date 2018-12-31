using HCL.Academy.Web.DAL;
using HCLAcademy.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using System.Text;
using HCLAcademy.Common;

namespace HCL.Academy.Web.Controllers
{
    public class TrainingAssessmentStatusController : Controller
    {
        // GET: TrainingAssessmentStatus
        public ActionResult Index()
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                TrainingStatus objTrainingStatus = new TrainingStatus();
                objTrainingStatus.Skills = dal.GetAllSkills();      //Gets a list of all skills.
                return View(objTrainingStatus);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ImportProject,UploadProjectDataFile", ex.Message, ex.StackTrace));
                return View();
            }
        }
        /// <summary>
        /// Gets the report in Excel format. 
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="competency"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        [SessionExpire]
        public FileResult DownloadReportToExcel(string skill, string competency)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            ExcelPackage excel = new ExcelPackage();
            var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
            workSheet.TabColor = System.Drawing.Color.Black;
            workSheet.DefaultRowHeight = 12;

            List<Training> SkillBasedTrainings = dal.GetTrainings(Convert.ToInt32(skill), Convert.ToInt32(competency));     //Get trainings based on the selected skill and competency.
            int k = 3;
            try
            {
                if (SkillBasedTrainings.Count > 0)
                {
                    workSheet.Row(1).Height = 40;
                    workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    workSheet.Row(1).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheet.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Row(1).Style.Font.Bold = true;
                    workSheet.Row(1).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);

                    workSheet.Row(2).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheet.Row(3).Style.VerticalAlignment = ExcelVerticalAlignment.Top;

                    workSheet.Cells[1, 1].Value = "Training Name";
                    workSheet.Cells[2, 1].Value = "Training Completed";
                    workSheet.Cells[2, 1].Style.Font.Bold = true;
                    workSheet.Cells[3, 1].Value = "Training Not Completed";
                    workSheet.Cells[3, 1].Style.Font.Bold = true;
                    workSheet.Column(1).Width = 28;

                    for (int i = 0; i < SkillBasedTrainings.Count; i++)
                    {
                        workSheet.Column(i + 2).Width = 50;
                        workSheet.Column(i + 2).Style.WrapText = true;
                        workSheet.Cells[1, i + 2].Value = SkillBasedTrainings[i].TrainingName;
                        k = 2;
                        List<UserTraining> userTrainings = dal.GetUserTrainingsByTrainingID(SkillBasedTrainings[i].TrainingId);

                        if (userTrainings != null & userTrainings.Count > 0)
                        {
                            StringBuilder completedUserList = new StringBuilder();
                            StringBuilder wipUserList = new StringBuilder();

                            foreach (UserTraining userTraining in userTrainings)
                            {
                                if (userTraining.IsTrainingCompleted)
                                {
                                    completedUserList.Append(userTraining.Employee);
                                    completedUserList.Append(";");
                                }
                                else
                                {
                                    wipUserList.Append(userTraining.Employee);
                                    wipUserList.Append(";");
                                }
                            }
                            workSheet.Cells[k, i + 2].Value = completedUserList.ToString();
                            k = k + 1;
                            workSheet.Cells[k, i + 2].Value = wipUserList.ToString();
                        }
                    }
                }
                //////////Get Assessments for the skill and competency level//////////
                List<Assessment> SkillBasedAssessments = dal.GetAssessments(Convert.ToInt32(skill), Convert.ToInt32(competency));

                if (SkillBasedAssessments.Count > 0)
                {
                    k = k + 3;
                    workSheet.Row(k).Height = 40;
                    workSheet.Row(k).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    workSheet.Row(k).Style.Fill.PatternType = ExcelFillStyle.Solid;
                    workSheet.Row(k).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SkyBlue);
                    workSheet.Row(k).Style.Font.Bold = true;
                    workSheet.Row(k).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheet.Row(k + 1).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheet.Row(k + 2).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    workSheet.Cells[k, 1].Value = "Assessment Name";
                    workSheet.Cells[k + 1, 1].Value = "Assessment Completed";
                    workSheet.Cells[k + 1, 1].Style.Font.Bold = true;
                    workSheet.Cells[k + 2, 1].Value = "Assessment Not Completed";
                    workSheet.Cells[k + 2, 1].Style.Font.Bold = true;

                    for (int i = 0; i < SkillBasedAssessments.Count; i++)
                    {
                        workSheet.Cells[k, i + 2].Value = SkillBasedAssessments[i].AssessmentName;
                        List<UserAssessment> userAssessments = dal.GetUserAssessmentsByAssessmentId(SkillBasedAssessments[i].AssessmentId);

                        string userlist = String.Empty;
                        if (userAssessments != null & userAssessments.Count > 0)
                        {
                            StringBuilder completedAssmtUserList = new StringBuilder();
                            StringBuilder wipAssmtUserList = new StringBuilder();
                            foreach (UserAssessment userAssessment in userAssessments)
                            {
                                if (userAssessment.IsAssessmentComplete)
                                {
                                    completedAssmtUserList.Append(userAssessment.Employee);
                                    completedAssmtUserList.Append(";");
                                }
                                else
                                {
                                    wipAssmtUserList.Append(userAssessment.Employee);
                                    wipAssmtUserList.Append(";");
                                }
                            }
                            workSheet.Cells[k + 1, i + 2].Value = completedAssmtUserList.ToString();
                            workSheet.Cells[k + 2, i + 2].Value = wipAssmtUserList.ToString();
                        }
                    }
                }
                string excelName = "Training.xlsx";
                using (var memoryStream = new MemoryStream())
                {
                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    Response.AddHeader("content-disposition", "attachment; filename=" + excelName);
                    excel.SaveAs(memoryStream);
                    memoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();
                    return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "TrainingAssessmentStatus,DownloadReportToExcel", ex.Message, ex.StackTrace));
                return null;
            }
        }


    }
}