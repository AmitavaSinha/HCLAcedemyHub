using ExcelDataReader;
using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.CustomFilters;
using HCLAcademy.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Configuration;

namespace HCLAcademy.Controllers
{
    [CustomAuthorizationFilter]
    public class AdminController : Controller
    {

        #region PUBLIC METHODS
        private readonly IDAL _dal;
        public AdminController(IDAL dal)
        {
            _dal = dal= (new DALFactory()).GetInstance();
        }

        // GET: Admin
        [Authorize]
        [SessionExpire]
        public ActionResult OnBoardAdmin()
        {
            UserOnBoarding objUserOnBoarding = new UserOnBoarding();
            
            objUserOnBoarding.Skills = _dal.GetAllSkills();
            objUserOnBoarding.Projects = _dal.GetAllProjects();
            return View(objUserOnBoarding);
        }
        /// <summary>
        /// Upload the selected file of users
        /// </summary>
        /// <param name="uploadedFile"></param>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [HttpPost]
        public JsonResult UploadOnboardFile(HttpPostedFileBase uploadedFile)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            SharePointDAL spDal = new SharePointDAL();

            StringBuilder logText = new StringBuilder();
            logText.Append("<table border = '1'> <tr><th>Result</th></tr>");

            if (uploadedFile != null && uploadedFile.ContentLength > 0)
            {
                #region Read file data
                if ((uploadedFile.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" || uploadedFile.ContentType == "application/octet-stream") &&
                    (uploadedFile.FileName.EndsWith(".xls") || uploadedFile.FileName.EndsWith(".xlsx")))
                {
                    //ExcelDataReader works on binary excel file
                    Stream stream = uploadedFile.InputStream;

                    IExcelDataReader reader = null;
                    if (uploadedFile.FileName.EndsWith(".xls"))
                    {
                        //reads the excel file with .xls extension
                        reader = ExcelReaderFactory.CreateBinaryReader(stream);
                    }
                    else //if (uploadedFile.FileName.EndsWith(".xlsx"))
                    {
                        //reads excel file with .xlsx extension
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                    }

                    //treats the first row of excel file as Coluymn Names
                 //   reader.IsFirstRowAsColumnNames = true;
                    //Adding reader data to DataSet()
                    DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });

                    reader.Close();

                    DataTable dt = result.Tables[0];

                    dt = dt.Rows.Cast<DataRow>().Where(row => !row.ItemArray.All(field => field is DBNull || string.IsNullOrWhiteSpace(field as string))).CopyToDataTable();

                    int i = 0;
                    int j = 0;

                    List<Skill> allSkill = dal.GetAllSkills();
                    List<Competence> allCompetence = dal.GetAllCompetenceList();
                    List<GEO> allGEO = dal.GetAllGEOs();
                    List<Role> allRole = dal.GetAllRoles();

                    int length = dt.Rows.Count;

                    foreach (DataRow item in dt.Rows)
                    {
                        #region Parse XLS File Data

                        UserManager user = null;
                        string emailId = Convert.ToString(dt.Rows[i][0]);
                        string competence = Convert.ToString(dt.Rows[i][2]);
                        string skillName = Convert.ToString(dt.Rows[i][1]);
                        string geo = Convert.ToString(dt.Rows[i][4]);
                        string role = Convert.ToString(dt.Rows[i][5]);

                        int skillId = 0;

                        if (allSkill.Exists(s => s.SkillName == skillName))
                        {
                            skillId = allSkill.Where(x => x.SkillName == skillName).FirstOrDefault().SkillId;

                            if (allCompetence.Exists(c => c.CompetenceName == competence && c.SkillId == skillId))
                            {
                                int competenceId = allCompetence.Where(c => c.CompetenceName == competence && c.SkillId == skillId).FirstOrDefault().CompetenceId;

                                if(allGEO.Exists(s => s.Title == geo))
                                {
                                    int geoId = allGEO.Where(x => x.Title == geo).FirstOrDefault().Id;

                                    if (allRole.Exists(r => r.Title == role))
                                    {
                                        int roleId = allRole.Where(x => x.Title == role).FirstOrDefault().Id;

                                        if (!string.IsNullOrWhiteSpace(emailId))
                                        {
                                            user = spDal.GetUserByEmail(emailId);
                                        }

                                        #region Valid user
                                        if (user != null)
                                        {
                                            // Search user in AcademyOnboarding list
                                            UserOnBoarding userOnboarding = dal.GetOnBoardingDetailsForUser(user);

                                            //if exist in Onboarding list
                                            if (!userOnboarding.IsPresentInOnBoard)
                                            {
                                                // Add user in ING Member group
                                                if (spDal.AddUserToGroup(user.EmailID))
                                                {
                                                    try
                                                    {
                                                        // Add User in AcademyOnboarding list 
                                                        
                                                        string dataStore = ConfigurationManager.AppSettings["DATASTORE"].ToString();

                                                        if(dataStore == "SqlSvr")
                                                        {
                                                            SqlSvrDAL sqlDAL = new SqlSvrDAL();
                                                            int dbUserId = sqlDAL.GetUserId(user.EmailID);
                                                            sqlDAL.OnBoardUser(competenceId.ToString(), skillId, user.SPUserId, geoId.ToString(), roleId, user.EmailID, user.UserName);
                                                            sqlDAL.OnboardEmail(user.EmailID, dbUserId, user.UserName);                                            
                                                        }
                                                        else
                                                        {
                                                            dal.OnBoardUser(competenceId.ToString(), skillId, user.SPUserId, geoId.ToString(), roleId, null, null);
                                                            dal.OnboardEmail(user.EmailID, user.SPUserId, user.UserName);
                                                        }
                                                        
                                                        j = i + 1;
                                                        logText.Append("<tr><td>Member with email id " + emailId + "successfully onboarded<td><tr>");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        UserManager loggeduser = (UserManager)Session["CurrentUser"];
                                                        LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, loggeduser.EmailID.ToString(), AppConstant.ApplicationName, "Admin, UploadOnboardFile", ex.Message, ex.StackTrace));
                                                        j = i + 1;
                                                        logText.Append("<tr><td>Exception occured while boarding user " + emailId + " at row no " + j + " Error: " + ex.Message + "<td><tr>");
                                                    }
                                                }
                                                else
                                                {
                                                    j = i + 1;
                                                    logText.Append("<tr><td>Error while adding User to the member group with email id " + emailId + " at row no " + j + "<td><tr>");
                                                }
                                            }
                                            else
                                            {
                                                j = i + 1;
                                                logText.Append("<tr><td>User already onboarded with email id " + emailId + " at row no " + j + "<td><tr>");
                                            }
                                        }
                                        else
                                        {
                                            j = i + 1;
                                            logText.Append("<tr><td>Invalid Email id " + emailId + " at row " + j + "<td><tr>");
                                        }
                                    }
                                }
                                #endregion Valid user
                            }
                            else
                            {
                                j = i + 1;
                                logText.Append("<tr><td>Invalid Competence " + emailId + " at row no " + j + "<td><tr>");
                            }
                        }
                        else
                        {
                            j = i + 1;
                            logText.Append("<tr><td>Invalid Skill " + emailId + " at row no " + j + "<td><tr>");
                        }

                        i++;

                        #endregion XLS file Parsing completed
                    }
                }
                #endregion Read file data
                else
                {
                    logText.Append("<tr><td>Please upload correct file format<td><tr>");
                }
            }
            else
            {
                logText.Append("<tr><td>Please upload correct file format<td><tr>");
            }

            logText.Append("</table>");

            return Json(new
            {
                statusCode = 200,
                status = true,
                message = logText.ToString(),
            }, JsonRequestBehavior.AllowGet);

        }
        /// <summary>
        /// Gets user details by seraching for a keyword in Emails
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult EmailSearch(string keyword)
        {
            UserManager user = null;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                SharePointDAL spDal = new SharePointDAL();
                user = spDal.GetUserByEmail(keyword);
            }
            return Json(user);
        }
        /// <summary>
        /// Onboarding process for a particular user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public ActionResult UserOnBoarding(UserManager user)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            UserOnBoarding objUserOnBoarding = new UserOnBoarding();
            try
            {
                if (user != null)
                {
                    objUserOnBoarding = dal.GetOnBoardingDetailsForUser(user);      //Gets onboarding details
                    objUserOnBoarding.Skills = dal.GetAllSkills();                  //List of all Skills
                    objUserOnBoarding.Competence = dal.GetAllCompetenceList();      //List of all Competencies
                    objUserOnBoarding.Status = Utilities.GetAllStatus();            //List of statuses
                    objUserOnBoarding.BGVStatus = Utilities.GetAllBGVStatus();      //Get the background verification status
                    objUserOnBoarding.ProfileSharingStatus = Utilities.GetAllProfileSharingStatus();    //Gets Profile sharing status
                    objUserOnBoarding.GEOs = dal.GetAllGEOs();                     //List of all GEOs
                    objUserOnBoarding.Roles = dal.GetAllRoles();                      //List of all Roles
                }
                else
                {
                    objUserOnBoarding = null;
                }
              
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Admin, UserOnBoarding", ex.Message, ex.StackTrace));

            }
            return PartialView("_UserOnBoarding", objUserOnBoarding);
        }
        /// <summary>
        /// Gets all the Trainings and Assessments assigned to a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public ActionResult UserTrainingAndAssessment(UserManager user)
        {
            UserOnBoarding objUserOnBoarding = new UserOnBoarding();
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();

                if (user != null)
                {
                    objUserOnBoarding.Skills = dal.GetAllSkills();
                    objUserOnBoarding.Competence = dal.GetAllCompetenceList();
                    string dataStore = ConfigurationManager.AppSettings["DATASTORE"].ToString();
                    if(dataStore == "SqlSvr")
                    {
                        objUserOnBoarding.UserTrainings = dal.GetTrainingForUser(user.DBUserId);    
                        objUserOnBoarding.UserAssessments = dal.GetAssessmentForUser(user.DBUserId);
                    }
                    else
                    {
                        objUserOnBoarding.UserTrainings = dal.GetTrainingForUser(user.SPUserId);
                        objUserOnBoarding.UserAssessments = dal.GetAssessmentForUser(user.SPUserId);
                    }
                }
                else
                {
                    objUserOnBoarding = null;
                }
                
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Admin, UserTrainingAndAssessment", ex.Message, ex.StackTrace));
            }
            return PartialView("_UserTrainingAndAssessment", objUserOnBoarding);
        }
        /// <summary>
        /// Adds a skill to a particular user
        /// </summary>
        /// <param name="email"></param>
        /// <param name="userId"></param>
        /// <param name="skillId"></param>
        /// <param name="competence"></param>
        /// <param name="ismandatory"></param>
        /// <param name="lastdayofcompletion"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public bool AddSkill(string email, string userId, string skillId, string competence, bool ismandatory, DateTime lastdayofcompletion)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            return dal.AddSkill(email, userId, skillId, competence, ismandatory, lastdayofcompletion);
        }
        /// <summary>
        /// Adds a skill to a particular user
        /// </summary>
        /// <param name="email"></param>
        /// <param name="userId"></param>
        /// <param name="rolelId"></param>        
        /// <param name="ismandatory"></param>
        /// <param name="lastdayofcompletion"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public bool AddRole(string email, string userId, string roleId, bool ismandatory, DateTime lastdayofcompletion)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            return dal.AddRole(email, userId, roleId, ismandatory, lastdayofcompletion);
        }
        /// <summary>
        /// Fetches users of a particular status in a project
        /// </summary>
        /// <param name="status"></param>
        /// <param name="project"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public ActionResult UserOnBoardingReport(string status, int project)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<UserOnBoarding> lstUserOnBoarding = dal.GetOnBoardingDetailsReport(status, false);
            List<UserOnBoarding> lstUserOnBoardingData = null;
            try
            {
                
                if (project != 0)
                {
                    lstUserOnBoardingData = lstUserOnBoarding.Where(item => item.ProjectId == project).ToList();
                }
                else
                {
                    lstUserOnBoardingData = lstUserOnBoarding;
                }
                ViewBag.Skills = dal.GetAllSkills();
                ViewBag.Competence = dal.GetAllCompetenceList();
                ViewBag.Roles= dal.GetAllRoles();
                
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Admin, UserOnBoardingReport", ex.Message, ex.StackTrace));              
            }
            return PartialView("_UserOnBoardingReport", lstUserOnBoardingData);

        }
        /// <summary>
        /// Get trainings assigned for the selected user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult GetTrainingsForReport(int userId, string userEmail)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<UserTraining> trainings = dal.GetTrainingForUser(userId, false);
            return new JsonResult { Data = trainings };
        }
        /// <summary>
        /// Get Skills assigned for the selected user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult GetSkillsForReport(int userId, string userEmail)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<UserSkill> skills = dal.GetSkillForOnboardedUser(userId);
            return new JsonResult { Data = skills };
        }
        /// <summary>
        /// Get Skills assigned for the selected user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult GetRolesForReport(int userId, string userEmail)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<UserRole> userRoles = dal.GetRoleForOnboardedUser(userId);
            return new JsonResult { Data = userRoles };
        }
        /// <summary>
        /// Get Assessments assigned for the selected user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult GetAssessmentsForReport(int userId, string userEmail)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<UserAssessment> assessments = dal.GetAssessmentForUser(userId, false);
            return new JsonResult { Data = assessments };
        }
        /// <summary>
        /// Update the selected skill and assign new competence/completion date for a user
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="competence"></param>
        /// <param name="userId"></param>
        /// <param name="completiondate"></param>
        /// <param name="isCompetenceChanged"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult UpdateUserSkill(int itemId, string competence, int userId, DateTime completiondate, bool isCompetenceChanged)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            try
            {
                dal.UpdateUserSkill(itemId, competence, userId, completiondate, isCompetenceChanged);
                return new JsonResult { Data = true };
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "AcademyVideo, UpdateUserSkill", ex.Message, ex.StackTrace));
                return new JsonResult { Data = false };
            }
        }

        public ActionResult FakeAjaxCall()
        {
            return null;
        }
        /// <summary>
        /// Download a report for a particular status in Excel format
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        [SessionExpire]
        public FileResult DownloadReportToExcel(string status)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                List<UserOnBoarding> lstUserOnBoarding = dal.GetOnBoardingDetailsReport(status, true);
                ExcelPackage excel = new ExcelPackage();
                var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
                workSheet.TabColor = System.Drawing.Color.Black;
                workSheet.DefaultRowHeight = 12;
                //Header of table  
                //  
                workSheet.Row(1).Height = 20;

                workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Row(1).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                workSheet.Row(1).Style.Fill.PatternType = ExcelFillStyle.Solid;
                workSheet.Row(1).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DarkGray);
                workSheet.Row(1).Style.Font.Bold = true;


                workSheet.Row(2).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Row(2).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                workSheet.Row(2).Style.Fill.PatternType = ExcelFillStyle.Solid;
                workSheet.Row(2).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Gray);
                workSheet.Row(2).Style.Font.Bold = true;

                workSheet.Cells[1, 1].Value = "Name";
                workSheet.Cells[1, 2].Value = "Email";
                workSheet.Cells[1, 3].Value = "Skill";
                workSheet.Cells[1, 4].Value = "Competence";
                workSheet.Cells[1, 5].Value = "Trainings";
                workSheet.Cells[1, 5, 1, 7].Merge = true;
                // Sub Header for Training
                workSheet.Cells[2, 5].Value = "Name";
                workSheet.Cells[2, 6].Value = "Course Name";
                workSheet.Cells[2, 7].Value = "Status";

                workSheet.Cells[1, 8].Value = "Assessments";
                workSheet.Cells[1, 8, 1, 10].Merge = true;
                // Sub Header for Assessment
                workSheet.Cells[2, 8].Value = "Name";
                workSheet.Cells[2, 9].Value = "Course Name";
                workSheet.Cells[2, 10].Value = "Status";

                // Applying Border to Header Cell
                System.Drawing.Color color = System.Drawing.Color.Black;

                workSheet.Cells[1, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                workSheet.Cells[1, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                workSheet.Cells[1, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                workSheet.Cells[1, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                workSheet.Cells[1, 5, 1, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                workSheet.Cells[1, 8, 1, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                workSheet.Cells[2, 5, 2, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                workSheet.Cells[2, 8, 2, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);

                //Body of table  
                //  
                int recordIndex = 3;

                string strText = string.Empty;
                string itemStatus = string.Empty;
                foreach (var user in lstUserOnBoarding)
                {
                    workSheet.Row(recordIndex).Style.VerticalAlignment = ExcelVerticalAlignment.Top;

                    int trainingCount = user.UserTrainings.Count;
                    int assessmentCount = user.UserAssessments.Count;

                    workSheet.Cells[recordIndex, 1].Value = user.Name;
                    workSheet.Cells[recordIndex, 2].Value = user.Email;

                    int i = 0;
                    foreach (var item in user.UserTrainings)
                    {

                        itemStatus = item.IsTrainingCompleted == true ? "Completed" : "Not Completed";
                        strText += item.SkillName + " --> " + item.TrainingName + "; Status :" + itemStatus + "; Color :" + item.StatusColor + " \r\n";

                        System.Drawing.Color cellColor = System.Drawing.Color.FromName(item.StatusColor);

                        workSheet.Cells[recordIndex + i, 5].Value = item.TrainingName;
                        workSheet.Cells[recordIndex + i, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        workSheet.Cells[recordIndex + i, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        workSheet.Cells[recordIndex + i, 5].Style.Fill.BackgroundColor.SetColor(cellColor);

                        workSheet.Cells[recordIndex + i, 6].Value = item.SkillName;
                        workSheet.Cells[recordIndex + i, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        workSheet.Cells[recordIndex + i, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        workSheet.Cells[recordIndex + i, 6].Style.Fill.BackgroundColor.SetColor(cellColor);

                        workSheet.Cells[recordIndex + i, 7].Value = item.ItemStatus;
                        workSheet.Cells[recordIndex + i, 7].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        workSheet.Cells[recordIndex + i, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        workSheet.Cells[recordIndex + i, 7].Style.Fill.BackgroundColor.SetColor(cellColor);

                        i++;

                    }

                    strText = string.Empty;
                    itemStatus = string.Empty;
                    int j = 0;
                    foreach (var item in user.UserAssessments)
                    {
                        itemStatus = item.IsAssessmentComplete == true ? "Completed" : "Not Completed";
                        strText += item.TrainingAssessment + "; Status :" + itemStatus + "\r\n" + " (" + item.SkillName + " --> " + item.TrainingName + ") " + " \r\n";
                        System.Drawing.Color cellColor = System.Drawing.Color.FromName(item.StatusColor);

                        workSheet.Cells[recordIndex + j, 8].Value = item.TrainingAssessment;
                        workSheet.Cells[recordIndex + j, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        workSheet.Cells[recordIndex + j, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        workSheet.Cells[recordIndex + j, 8].Style.Fill.BackgroundColor.SetColor(cellColor);

                        workSheet.Cells[recordIndex + j, 9].Value = item.SkillName;
                        workSheet.Cells[recordIndex + j, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        workSheet.Cells[recordIndex + j, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        workSheet.Cells[recordIndex + j, 9].Style.Fill.BackgroundColor.SetColor(cellColor);

                        workSheet.Cells[recordIndex + j, 10].Value = item.ItemStatus;
                        workSheet.Cells[recordIndex + j, 10].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        workSheet.Cells[recordIndex + j, 10].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        workSheet.Cells[recordIndex + j, 10].Style.Fill.BackgroundColor.SetColor(cellColor);

                        j++;
                    }
                    int k = 0;
                    foreach (var item in user.UserSkills)
                    {
                        workSheet.Cells[recordIndex + k, 3].Value = item.Skill;
                        workSheet.Cells[recordIndex + k, 3].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        workSheet.Cells[recordIndex + k, 4].Value = item.Competence;
                        workSheet.Cells[recordIndex + k, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        workSheet.Cells[recordIndex + k, 3].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.White);
                        workSheet.Cells[recordIndex + k, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        workSheet.Cells[recordIndex + k, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.White);
                        workSheet.Cells[recordIndex + k, 4].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        k++;
                    }

                    // Applying Style to the cell
                    int rowCount = trainingCount > assessmentCount ? trainingCount : assessmentCount;

                    workSheet.Cells[recordIndex, 1, (recordIndex + rowCount) - 1, 1].Merge = true;
                    workSheet.Cells[recordIndex, 2, (recordIndex + rowCount) - 1, 2].Merge = true;

                    workSheet.Cells[recordIndex, 1, (recordIndex + rowCount) - 1, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                    workSheet.Cells[recordIndex, 2, (recordIndex + rowCount) - 1, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                    workSheet.Cells[recordIndex, 3, (recordIndex + rowCount) - 1, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                    workSheet.Cells[recordIndex, 4, (recordIndex + rowCount) - 1, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                    workSheet.Cells[recordIndex, 5, (recordIndex + rowCount) - 1, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);
                    workSheet.Cells[recordIndex, 8, (recordIndex + rowCount) - 1, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin, color);

                    recordIndex = recordIndex + rowCount;
                }
                workSheet.Column(4).Style.WrapText = true;
                workSheet.Column(5).Style.WrapText = true;
                workSheet.Column(6).Style.WrapText = true;

                workSheet.Column(1).Width = 30;
                workSheet.Column(2).Width = 30;
                workSheet.Column(3).Width = 30;
                workSheet.Column(4).Width = 30;
                workSheet.Column(5).Width = 40;
                workSheet.Column(6).Width = 15;
                workSheet.Column(7).Width = 17;
                workSheet.Column(8).Width = 40;
                workSheet.Column(9).Width = 15;
                workSheet.Column(10).Width = 19;

                string excelName = status + ".xlsx";

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
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Admin, DownloadReportToExcel", ex.Message, ex.StackTrace));
                return null;
            }
        }
        /// <summary>
        /// Download a report for a particular status in PDF format
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        [SessionExpire]
        public FileResult DownloadReportToPDF(string status)
        {
            MemoryStream workStream = new MemoryStream();
            string strPDFFileName = string.Format(status + ".pdf");
            try
            {

                // Create a new MigraDoc document

                Document document = new Document();
                document.Info.Title = "A sample report";

                document.Info.Subject = "Report for user Onboarding.";

                document.Info.Author = "HCL";

                DefineStyles(document);

                CreatePage(document, status);

                // Create a renderer for the MigraDoc document.

                PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer();

                // Associate the MigraDoc document with a renderer

                pdfRenderer.Document = document;

                // Layout and render document to PDF

                pdfRenderer.RenderDocument();

                // Save the document...
                pdfRenderer.PdfDocument.Save(workStream, false);

                // ...and start a viewer.
                byte[] byteInfo = workStream.ToArray();
                workStream.Write(byteInfo, 0, byteInfo.Length);
                workStream.Position = 0;

            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Admin, DownloadReportToPDF", ex.Message, ex.StackTrace));
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }

            return File(workStream, "application/pdf", strPDFFileName);
        }
        /// <summary>
        /// On board a user with the selected skills, competence,email and GEO
        /// </summary>
        /// <param name="competence"></param>
        /// <param name="skillId"></param>
        /// <param name="userId"></param>
        /// <param name="email"></param>
        /// <param name="geo"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult OnBoardUser(string competence, int skillId, int userId, string email, string geo,int roleId)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            SharePointDAL spDal = new SharePointDAL();
            string result = string.Empty;

            try
            {
                spDal.AddUserToGroup(email);              
                UserManager UserMgr = spDal.GetUserByEmail(email);
                string dataStore = ConfigurationManager.AppSettings["DATASTORE"].ToString();
                if(dataStore == "SqlSvr")
                {
                    SqlSvrDAL sqlDAL = new SqlSvrDAL();
                    
                    sqlDAL.OnBoardUser(competence, skillId, userId, geo, roleId, UserMgr.EmailID, UserMgr.UserName);
                    int dbUserId = sqlDAL.GetUserId(email);
                    sqlDAL.OnboardEmail(UserMgr.EmailID, dbUserId, UserMgr.UserName);
                    
                }
                else
                {
                    dal.OnBoardUser(competence, skillId, userId, geo, roleId, null, null);
                    dal.OnboardEmail(UserMgr.EmailID, UserMgr.SPUserId, UserMgr.UserName);                    
                }               
                result = "Onboarding Successful!!";
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Admin, OnBoardUser", ex.Message, ex.StackTrace));
                result = "Error while Onboarding..." + ex.Message;
            }

            return new JsonResult { Data = result };
        }
        /// <summary>
        /// Edit details of an on boarded user
        /// </summary>
        /// <param name="objUserOnboard"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult EditOnBoardUser(UserOnBoarding objUserOnboard)
        {
            bool result = false;
            return new JsonResult { Data = result };
        }
        /// <summary>
        /// Get all the competences for a particular skill ID
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        public JsonResult FillCompetence(int Id)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                List<Competence> competencies = dal.GetCompetenciesBySkillId(Id);
                JsonResult j = Json(competencies, JsonRequestBehavior.AllowGet);
                return j;
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Admin, OnBoardUser", ex.Message, ex.StackTrace));
                return Json("", JsonRequestBehavior.AllowGet);
            }
            

        }

        /// <summary>
        /// Get all the competences for a skill using the name of the skill
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        public JsonResult FillCompetenceBySkillName(string name)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                List<Competence> competencies = dal.GetCompetenciesBySkillName(name);
                if (competencies.Count > 0)
                {
                    JsonResult j = Json(competencies, JsonRequestBehavior.AllowGet);
                    return j;
                }
                else
                {
                    return Json("", JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Admin, FillCompetenceBySkillName", ex.Message, ex.StackTrace));
                return Json("", JsonRequestBehavior.AllowGet);
            }

        }
        /// <summary>
        /// Get the list of trainings based on the selected skill and competency
        /// </summary>
        /// <param name="competence"></param>
        /// <param name="competenceId"></param>
        /// <param name="skillId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult GetTrainingOnSkillAndCompetence(string competence, int competenceId, int skillId)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<Training> trainings = dal.GetTrainings(skillId, competenceId);
            return new JsonResult { Data = trainings };
        }
        /// <summary>
        /// Assign a particular training to a user
        /// </summary>
        /// <param name="trainings"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult AssignTrainingsToUser(List<UserTraining> trainings, int userId)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            bool result = dal.AssignTrainingsToUser(trainings, userId);
            return new JsonResult { Data = result };
        }
        /// <summary>
        /// Get the list of Assessments based on the selected skill and competency
        /// </summary>
        /// <param name="competence"></param>
        /// <param name="competenceId"></param>
        /// <param name="skillId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult GetAssessmentOnSkillAndCompetence(string competence, int competenceId, int skillId)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<Assessment> assessment = dal.GetAssessments(skillId, competenceId);
            return new JsonResult { Data = assessment };
        }
        /// <summary>
        /// Assign a particular assessment to a user
        /// </summary>
        /// <param name="assessments"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult AssignAssessmentsToUser(List<UserAssessment> assessments, int userId)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            bool result = dal.AssignAssessmentsToUser(assessments, userId);
            return new JsonResult { Data = result };
        }
        /// <summary>
        /// Assign an assessment to all users
        /// </summary>
        /// <param name="assignedGroup"></param>
        /// <param name="assessment"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult AssignAssessmentsToAllUser(int assignedGroup, UserAssessment assessment)
        {
            
            IDAL dal = (new DALFactory()).GetInstance();
            bool result = false;
            try
            {
                List<UserManager> lstUserOnBoarding = dal.GetAllOnBoardedUser(assignedGroup);
                List<UserAssessment> assessments = new List<UserAssessment>();
                assessments.Add(assessment);

                foreach (UserManager user in lstUserOnBoarding)
                {
                    string dataStore = ConfigurationManager.AppSettings["DATASTORE"].ToString();
                    if (dataStore == "SqlSvr")
                    {
                        result = dal.AssignAssessmentsToUser(assessments, user.DBUserId);

                    }
                    else
                    {
                        result = dal.AssignAssessmentsToUser(assessments, user.SPUserId);

                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Admin, AssignAssessmentsToAllUser", ex.Message, ex.StackTrace));
                //return Json("", JsonRequestBehavior.AllowGet);
            }

            return new JsonResult { Data = result };
        }
        /// <summary>
        /// Assign a training to a particular group of users
        /// </summary>
        /// <param name="assignedGroup"></param>
        /// <param name="training"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult AssignTrainingsToAllUser(int assignedGroup, UserTraining training)
        {
            IDAL dal = (new DALFactory()).GetInstance();

            bool result = false;
            try
            {
                List<UserManager> lstUserOnBoarding = dal.GetAllOnBoardedUser(assignedGroup);

                List<UserTraining> trainings = new List<UserTraining>();
                trainings.Add(training);
                string dataStore = ConfigurationManager.AppSettings["DATASTORE"].ToString();
                if (dataStore == "SqlSvr")
                {
                    foreach (UserManager user in lstUserOnBoarding)
                    {
                        result = dal.AssignTrainingsToUser(trainings, user.DBUserId);
                    }
                }
                else
                {
                    foreach (UserManager user in lstUserOnBoarding)
                    {
                        result = dal.AssignTrainingsToUser(trainings, user.SPUserId);
                    }

                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Admin, AssignAssessmentsToAllUser", ex.Message, ex.StackTrace));
            }
                return new JsonResult { Data = result };
        }
        /// <summary>
        /// Remove a particular skill from a user's set of skills
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult RemoveUserSkill(int itemId, string userId)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                dal.RemoveUserSkill(itemId, userId);
                return new JsonResult { Data = true };
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Admin, RemoveUserSkill", ex.Message, ex.StackTrace));

                return new JsonResult { Data = false };
            }
        }
        /// <summary>
        /// Remove a particular skill from a user's set of skills
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult RemoveUserRole(int itemId, string userId)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                dal.RemoveUserRole(itemId, userId);
                return new JsonResult { Data = true };
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Admin, RemoveUserSkill", ex.Message, ex.StackTrace));

                return new JsonResult { Data = false };
            }
        }
        #endregion

        #region PRIVATE METHODS
        /// <summary>
        /// Defines the style for a particular page
        /// </summary>
        /// <param name="document"></param>
        private void DefineStyles(Document document)
        {
            try
            {
                // Get the predefined style Normal.
                Style style = document.Styles["Normal"];
                // Because all styles are derived from Normal, the next line changes the 
                // font of the whole document. Or, more exactly, it changes the font of
                // all styles and paragraphs that do not redefine the font.
                style.Font.Name = "Verdana";

                style = document.Styles[StyleNames.Header];
                style.ParagraphFormat.AddTabStop("16cm", TabAlignment.Right);

                style = document.Styles[StyleNames.Footer];
                style.ParagraphFormat.AddTabStop("8cm", TabAlignment.Center);

                // Create a new style called Table based on style Normal
                style = document.Styles.AddStyle("Table", "Normal");
                style.Font.Name = "Verdana";
                style.Font.Name = "Times New Roman";
                style.Font.Size = 9;

                // Create a new style called Reference based on style Normal
                style = document.Styles.AddStyle("Reference", "Normal");
                style.ParagraphFormat.SpaceBefore = "5mm";
                style.ParagraphFormat.SpaceAfter = "5mm";
                style.ParagraphFormat.TabStops.AddTabStop("6cm", TabAlignment.Right);
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Admin, DefineStyles", ex.Message, ex.StackTrace));
              
            }
        }
        /// <summary>
        /// Create all pages
        /// </summary>
        /// <param name="document"></param>
        /// <param name="status"></param>
        private void CreatePage(Document document, string status)
        {
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();

                // Each MigraDoc document needs at least one section.

                List<UserOnBoarding> lstUserOnBoarding = dal.GetOnBoardingDetailsReport(status, true);

                // Add OnBoarding Report
                AddOnBoardingDetails(document, lstUserOnBoarding);

                // Add Training Details
                AddTrainingDetails(document, lstUserOnBoarding);

                //Add Assessment
                AddAssessmentDetails(document, lstUserOnBoarding);
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Admin, CreatePage", ex.Message, ex.StackTrace));

            }

        }
        /// <summary>
        /// Create the section to add Onboarding details
        /// </summary>
        /// <param name="document"></param>
        /// <param name="lstUserOnBoarding"></param>
        private void AddOnBoardingDetails(Document document, List<UserOnBoarding> lstUserOnBoarding)
        {
            try
            {
                Section section = document.AddSection();

                section.AddParagraph("User OnBoarding Report");

                section.AddParagraph("\n");

                // Create the item table

                Table table = section.AddTable();
                table.Style = "Table";
                table.Borders.Width = 0.2;
                table.Borders.Left.Width = 0.5;
                table.Borders.Right.Width = 0.5;
                table.Rows.LeftIndent = 0;

                // Before you can add a row, you must define the columns

                MigraDoc.DocumentObjectModel.Tables.Column column = table.AddColumn("5.5cm");
                column.Format.Alignment = ParagraphAlignment.Center;

                column = table.AddColumn("5cm");
                column.Format.Alignment = ParagraphAlignment.Right;

                column = table.AddColumn("4cm");
                column.Format.Alignment = ParagraphAlignment.Right;

                column = table.AddColumn("3cm");
                column.Format.Alignment = ParagraphAlignment.Right;

                //column = table.AddColumn("1.5cm");
                //column.Format.Alignment = ParagraphAlignment.Right;

                //column = table.AddColumn("1.5cm");
                //column.Format.Alignment = ParagraphAlignment.Right;

                // Create the header of the table

                Row row = table.AddRow();

                row.HeadingFormat = true;
                row.Format.Alignment = ParagraphAlignment.Center;
                row.Format.Font.Bold = true;
                row.Shading.Color = MigraDoc.DocumentObjectModel.Colors.Gray;

                //row.Shading.Color = TableBlue;

                row.Cells[0].AddParagraph("Name");
                row.Cells[1].AddParagraph("Email");
                row.Cells[2].AddParagraph("Primary Skill");
                row.Cells[3].AddParagraph("Competence");
                //   row.Cells[4].AddParagraph("BGV Status");
                //   row.Cells[5].AddParagraph("Profile Sharing");

                foreach (var item in lstUserOnBoarding)
                {
                    Row rowData = table.AddRow();
                    rowData.HeadingFormat = true;
                    rowData.Format.Alignment = ParagraphAlignment.Left;

                    rowData.Cells[0].AddParagraph(item.Name);
                    rowData.Cells[1].AddParagraph(item.Email);
                    rowData.Cells[2].AddParagraph(item.CurrentSkill);
                    rowData.Cells[3].AddParagraph(item.CurrentCompetance);
                    // rowData.Cells[4].AddParagraph(item.CurrentBGVStatus);
                    //   rowData.Cells[5].AddParagraph(item.CurrentProfileSharing);
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Admin, AddOnBoardingDetails", ex.Message, ex.StackTrace));

            }
        }
        /// <summary>
        /// Add the training details section on the Home Page
        /// </summary>
        /// <param name="document"></param>
        /// <param name="lstUserOnBoarding"></param>
        private void AddTrainingDetails(Document document, List<UserOnBoarding> lstUserOnBoarding)
        {
            try
            {
                Section section = document.AddSection();

                section.AddParagraph("Details Training Report");

                section.AddParagraph("\n");

                // Create the item table

                Table table = section.AddTable();
                table.Style = "Table";
                //table.Borders.Color = MigraDoc.DocumentObjectModel.Color.FromRgb;
                table.Borders.Width = 0.2;
                table.Borders.Left.Width = 0.5;
                table.Borders.Right.Width = 0.5;
                table.Rows.LeftIndent = 0;

                // Before you can add a row, you must define the columns

                MigraDoc.DocumentObjectModel.Tables.Column column = table.AddColumn("3cm");
                column.Format.Alignment = ParagraphAlignment.Center;

                column = table.AddColumn("4.7cm");
                column.Format.Alignment = ParagraphAlignment.Right;

                column = table.AddColumn("4cm");
                column.Format.Alignment = ParagraphAlignment.Right;

                column = table.AddColumn("2cm");
                column.Format.Alignment = ParagraphAlignment.Right;

                column = table.AddColumn("1.8cm");
                column.Format.Alignment = ParagraphAlignment.Right;

                // Create the header of the table

                Row row = table.AddRow();

                row.HeadingFormat = true;
                row.Format.Alignment = ParagraphAlignment.Center;
                row.Format.Font.Bold = true;
                row.Shading.Color = MigraDoc.DocumentObjectModel.Colors.Gray;

                //row.Shading.Color = TableBlue;

                row.Cells[0].AddParagraph("Name");
                row.Cells[1].AddParagraph("Email");
                row.Cells[2].AddParagraph("Training Name");
                row.Cells[3].AddParagraph("Skill");
                row.Cells[4].AddParagraph("Status");

                foreach (var item in lstUserOnBoarding)
                {
                    Row rowData = table.AddRow();
                    rowData.HeadingFormat = true;
                    rowData.Format.Alignment = ParagraphAlignment.Left;

                    rowData.Cells[0].AddParagraph(item.Name);
                    rowData.Cells[0].MergeDown = item.UserTrainings.Count;
                    rowData.Cells[1].AddParagraph(item.Email);
                    rowData.Cells[1].MergeDown = item.UserTrainings.Count;

                    foreach (var training in item.UserTrainings)
                    {
                        Row rowTraining = table.AddRow();
                        rowTraining.HeadingFormat = true;
                        rowTraining.Format.Alignment = ParagraphAlignment.Left;

                        rowTraining.Cells[2].AddParagraph(training.TrainingName);
                        rowTraining.Cells[2].Shading.Color = MigraDoc.DocumentObjectModel.Color.Parse(training.StatusColor);
                        rowTraining.Cells[3].AddParagraph(training.SkillName);
                        rowTraining.Cells[3].Shading.Color = MigraDoc.DocumentObjectModel.Color.Parse(training.StatusColor);
                        rowTraining.Cells[4].AddParagraph(training.ItemStatus);
                        rowTraining.Cells[4].Shading.Color = MigraDoc.DocumentObjectModel.Color.Parse(training.StatusColor);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Admin, AddTrainingDetails", ex.Message, ex.StackTrace));

            }
        }
        /// <summary>
        /// Add the Assessments section to the Home Page
        /// </summary>
        /// <param name="document"></param>
        /// <param name="lstUserOnBoarding"></param>
        private void AddAssessmentDetails(Document document, List<UserOnBoarding> lstUserOnBoarding)
        {
            try
            {
                Section section = document.AddSection();

                section.AddParagraph("Details Assessment Report");

                section.AddParagraph("\n");

                // Create the item table

                Table table = section.AddTable();
                table.Style = "Table";
                table.Borders.Width = 0.2;
                table.Borders.Left.Width = 0.5;
                table.Borders.Right.Width = 0.5;
                table.Rows.LeftIndent = 0;

                // Before you can add a row, you must define the columns

                MigraDoc.DocumentObjectModel.Tables.Column column = table.AddColumn("3cm");
                column.Format.Alignment = ParagraphAlignment.Center;

                column = table.AddColumn("4.7cm");
                column.Format.Alignment = ParagraphAlignment.Right;

                column = table.AddColumn("4cm");
                column.Format.Alignment = ParagraphAlignment.Right;

                column = table.AddColumn("2cm");
                column.Format.Alignment = ParagraphAlignment.Right;

                column = table.AddColumn("1.8cm");
                column.Format.Alignment = ParagraphAlignment.Right;

                // Create the header of the table

                Row row = table.AddRow();

                row.HeadingFormat = true;
                row.Format.Alignment = ParagraphAlignment.Center;
                row.Format.Font.Bold = true;
                row.Shading.Color = MigraDoc.DocumentObjectModel.Colors.Gray;

                //row.Shading.Color = TableBlue;

                row.Cells[0].AddParagraph("Name");
                row.Cells[1].AddParagraph("Email");
                row.Cells[2].AddParagraph("Assessment Name");
                row.Cells[3].AddParagraph("Skill");
                row.Cells[4].AddParagraph("Status");

                foreach (var item in lstUserOnBoarding)
                {
                    Row rowData = table.AddRow();
                    rowData.HeadingFormat = true;
                    rowData.Format.Alignment = ParagraphAlignment.Left;

                    rowData.Cells[0].AddParagraph(item.Name);
                    rowData.Cells[0].MergeDown = item.UserAssessments.Count;
                    rowData.Cells[1].AddParagraph(item.Email);
                    rowData.Cells[1].MergeDown = item.UserAssessments.Count;

                    foreach (UserAssessment assessment in item.UserAssessments)
                    {
                        Row rowTraining = table.AddRow();
                        rowTraining.HeadingFormat = true;
                        rowTraining.Format.Alignment = ParagraphAlignment.Left;

                        rowTraining.Cells[2].AddParagraph(assessment.TrainingAssessment);
                        rowTraining.Cells[2].Shading.Color = MigraDoc.DocumentObjectModel.Color.Parse(assessment.StatusColor);
                        rowTraining.Cells[3].AddParagraph(assessment.SkillName);
                        rowTraining.Cells[3].Shading.Color = MigraDoc.DocumentObjectModel.Color.Parse(assessment.StatusColor);
                        rowTraining.Cells[4].AddParagraph(assessment.ItemStatus);
                        rowTraining.Cells[4].Shading.Color = MigraDoc.DocumentObjectModel.Color.Parse(assessment.StatusColor);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Admin, AddAssessmentDetails", ex.Message, ex.StackTrace));

            }
        }
        /// <summary>
        /// get the list of Onboarded users based on selected status
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        private DataTable GetTable(string status)
        {
            DataTable table = new DataTable();
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();

                //
                List<UserOnBoarding> lstUserOnBoarding = dal.GetOnBoardingDetailsReport(status, true);
                //
                
                table.Columns.Add("Name", typeof(string));
                table.Columns.Add("Email", typeof(string));
                table.Columns.Add("Skill", typeof(string));
                table.Columns.Add("Competance", typeof(string));

                //
                // Here we add five DataRows.
                //
                foreach (UserOnBoarding item in lstUserOnBoarding)
                {
                    table.Rows.Add(item.Name, item.Email, item.CurrentSkill, item.CurrentCompetance);
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Admin, GetTable", ex.Message, ex.StackTrace));

            }
            return table;
        }


        #endregion
    }
}