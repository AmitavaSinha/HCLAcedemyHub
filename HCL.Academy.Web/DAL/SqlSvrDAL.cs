using HCL.Academy.Web.Models;
using HCLAcademy.Common;
using HCLAcademy.Models;
using HCLAcademy.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.Xml;
using System.Globalization;

namespace HCL.Academy.Web.DAL
{
    public class SqlSvrDAL : IDAL
    {
        private UserManager CurrentUser;
        private string CurrentSiteUrl;

        string strConnectionString = string.Empty;
        SqlConnection connection;


        public SqlSvrDAL()
        {
            this.strConnectionString = ConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;
            this.connection = new SqlConnection(strConnectionString);

            CurrentSiteUrl = ConfigurationManager.AppSettings["URL"].ToString();
            CurrentUser = (UserManager)System.Web.HttpContext.Current.Session["CurrentUser"];
            UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];

        }

        public List<AcademyVideo> GetAllAcademyVideos()
        {
            List<AcademyVideo> lstAcademyVideo = new List<AcademyVideo>();

            return lstAcademyVideo;
        }
        public List<QuestionDetail> GetEachQuestionDetails(string listName, int assessmentId)
        {
            List<QuestionDetail> allQuestions = new List<QuestionDetail>();
            try
            {
                QuestionDetail eachQuestionDetails = null;
                DataHelper dh = new DataHelper(strConnectionString);
                DataSet ds = new DataSet();

                try
                {
                    SqlParameter[] sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter();
                    sqlParameters[0].ParameterName = "@assessmentID";
                    sqlParameters[0].Value = assessmentId;
                    sqlParameters[0].Direction = ParameterDirection.Input;
                    ds = dh.ExecuteDataSet("[dbo].[proc_GetEachQuestionDetails]", CommandType.StoredProcedure, sqlParameters);

                }
                finally
                {
                    if (dh != null)
                    {
                        if (dh.DataConn != null)
                        {
                            dh.DataConn.Close();
                        }
                    }
                }
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow item in ds.Tables[0].Rows)
                        {
                            eachQuestionDetails = new QuestionDetail();
                            if (item["Question"] != null && !(item["Question"] is DBNull))
                            {
                                string multitextValue = item["Question"].ToString();
                                eachQuestionDetails.QuestionTitle = multitextValue;
                            }
                            if (item["Option1"] != null && !(item["Option1"] is DBNull))
                            {
                                eachQuestionDetails.Option1 = Convert.ToString(item["Option1"]);
                            }
                            if (item["Option2"] != null && !(item["Option2"] is DBNull))
                            {
                                eachQuestionDetails.Option2 = Convert.ToString(item["Option2"]);
                            }
                            if (item["Option3"] != null && !(item["Option3"] is DBNull))
                            {
                                eachQuestionDetails.Option3 = Convert.ToString(item["Option3"]);
                            }
                            if (item["Option4"] != null && !(item["Option4"] is DBNull))
                            {
                                eachQuestionDetails.Option4 = Convert.ToString(item["Option4"]);
                            }
                            if (item["Option5"] != null && !(item["Option5"] is DBNull))
                            {
                                if (item["Option5"].ToString().Length > 0)
                                    eachQuestionDetails.Option5 = item["Option5"].ToString();
                            }
                            int CorrectOptionSequence = 0;
                            if (item["Option5"] != null && !(item["Option5"] is DBNull))
                            {
                                CorrectOptionSequence = Convert.ToInt32(item["CorrectOptionSequence"]);
                            }
                            if (CorrectOptionSequence == 1)
                            {
                                eachQuestionDetails.CorrectOption = eachQuestionDetails.Option1;
                            }
                            else if (CorrectOptionSequence == 2)
                            {
                                eachQuestionDetails.CorrectOption = eachQuestionDetails.Option2;
                            }
                            else if (CorrectOptionSequence == 3)
                            {
                                eachQuestionDetails.CorrectOption = eachQuestionDetails.Option3;
                            }
                            else if (CorrectOptionSequence == 4)
                            {
                                eachQuestionDetails.CorrectOption = eachQuestionDetails.Option4;
                            }
                            else
                            {
                                eachQuestionDetails.CorrectOption = Convert.ToString(item["CorrectOption"]);
                            }
                            eachQuestionDetails.Marks = Convert.ToString(item["Marks"]);
                            eachQuestionDetails.QuestionID = Convert.ToString(item["ID"]);
                            allQuestions.Add(eachQuestionDetails);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetEachQuestionDetails", ex.Message, ex.StackTrace));
            }
            return allQuestions;
        }

        public void RemoveUserSkill(int itemId, string userId)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@userId", userId), new SqlParameter("@itemId", itemId) };
                DataHelper dh = new DataHelper(strConnectionString);
                dh.ExecuteNonQuery("[dbo].[proc_RemoveUserSkill]", CommandType.StoredProcedure, parameters);

            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
        }
        public List<Role> GetAllRoles()
        {
            List<Role> roles = HttpContext.Current.Session[AppConstant.AllRoleData] as List<Role>;

            if (roles == null)
            {
                roles = new List<Role>();
                DataSet ds = new DataSet();
                DataHelper dh = new DataHelper(strConnectionString);

                try
                {
                    ds = dh.ExecuteDataSet("[dbo].[proc_GetAllRoles]", CommandType.StoredProcedure);

                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllProjects", ex.Message, ex.StackTrace));
                }
                finally
                {
                    if (dh != null)
                    {
                        if (dh.DataConn != null)
                        {
                            dh.DataConn.Close();
                        }
                    }
                }

                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                        {
                            Role r = new Role();
                            r.Id = Convert.ToInt32(ds.Tables[0].Rows[i]["RoleId"].ToString());
                            r.Title = ds.Tables[0].Rows[i]["RoleName"].ToString();
                            roles.Add(r);
                        }

                    }
                }

                HttpContext.Current.Session[AppConstant.AllRoleData] = roles;
            }
            return roles;

        }
        public FileStreamResult GetVideoStream(string url)
        {
            Stream inputStream = null;
            return new FileStreamResult(inputStream, "video/mp4");
        }

        public Assessments GetAssessmentDetails(int assessmentId)
        {
            Assessments assessment = new Assessments();
            try
            {
                List<AcademyJoinersCompletion> assessmentDetails = GetCurrentUserAssessments(AppConstant.UserAssessmentMapping, assessmentId, true);
                var objAssessment = assessmentDetails.SingleOrDefault();
                if (objAssessment.Attempts == objAssessment.MaxAttempts || objAssessment.AssessmentStatus)
                {
                    assessment.MaxAttemptsExceeded = objAssessment.Attempts == objAssessment.MaxAttempts ? true : false;
                    assessment.AssessmentCompletionStatus = objAssessment.AssessmentStatus;
                    return assessment;
                }
                assessment.AssessmentId = objAssessment.TrainingAssessmentLookUpId;
                assessment.AssessmentName = objAssessment.TrainingAssessmentLookUpText;
                DataHelper dh = new DataHelper(strConnectionString);
                DataSet ds = new DataSet();
                SqlParameter[] sqlParameters = new SqlParameter[1];
                sqlParameters[0] = new SqlParameter();
                sqlParameters[0].ParameterName = "@assessmentID";
                sqlParameters[0].Value = objAssessment.TrainingAssessmentLookUpId;
                sqlParameters[0].Direction = ParameterDirection.Input;
                ds = dh.ExecuteDataSet("[dbo].[proc_GetAssessment]", CommandType.StoredProcedure, sqlParameters);

                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        assessment.PassingPercentage = Convert.ToInt32(ds.Tables[0].Rows[0]["PassingMarks"].ToString());
                        assessmentDetails.SingleOrDefault().TrainingAssessmentTimeInMins = Convert.ToInt32(ds.Tables[0].Rows[0]["AssessmentTimeInMins"].ToString());
                    }

                }
                var questions = GetEachQuestionDetails(AppConstant.AcademyAssessment, objAssessment.TrainingAssessmentLookUpId);
                questions = questions.OrderBy(x => Guid.NewGuid()).Take(AppConstant.MaxQueForAssessment).ToList();
                var totalMarks = questions.Sum(x => Convert.ToInt32(x.Marks));
                assessment.QuestionDetails = questions;
                assessment.AssessmentDetails = assessmentDetails;
                assessment.TotalMarks = totalMarks;
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAssessmentDetails", ex.Message, ex.StackTrace));

            }
            return assessment;
        }

        public bool AssessmentResult(AssesmentResult result, List<QuestionDetail> questionDetails)
        {
            bool response = false;
            try
            {
                DataHelper dh = new DataHelper(strConnectionString);
                DataSet ds = new DataSet();

                DateTime CurrDate = DateTime.Now;

                foreach (var item in questionDetails)
                {
                    SqlParameter[] sqlParametersAssessmentAnswer = new SqlParameter[6];
                    sqlParametersAssessmentAnswer[0] = new SqlParameter();
                    sqlParametersAssessmentAnswer[0].ParameterName = "@assessmentId";
                    sqlParametersAssessmentAnswer[0].Value = result.AssessmentId;
                    sqlParametersAssessmentAnswer[0].Direction = ParameterDirection.Input;
                    sqlParametersAssessmentAnswer[1] = new SqlParameter();
                    sqlParametersAssessmentAnswer[1].ParameterName = "@userId";
                    sqlParametersAssessmentAnswer[1].Value = CurrentUser.DBUserId;
                    sqlParametersAssessmentAnswer[1].Direction = ParameterDirection.Input;
                    sqlParametersAssessmentAnswer[2] = new SqlParameter();
                    sqlParametersAssessmentAnswer[2].ParameterName = "@question";
                    sqlParametersAssessmentAnswer[2].Value = item.QuestionTitle;
                    sqlParametersAssessmentAnswer[2].Direction = ParameterDirection.Input;
                    sqlParametersAssessmentAnswer[3] = new SqlParameter();
                    sqlParametersAssessmentAnswer[3].ParameterName = "@correctOption";
                    sqlParametersAssessmentAnswer[3].Value = item.CorrectOption;
                    sqlParametersAssessmentAnswer[3].Direction = ParameterDirection.Input;
                    sqlParametersAssessmentAnswer[4] = new SqlParameter();
                    sqlParametersAssessmentAnswer[4].ParameterName = "@selectedOption";
                    sqlParametersAssessmentAnswer[4].Value = Convert.ToString(item.SelectedOption);
                    sqlParametersAssessmentAnswer[4].Direction = ParameterDirection.Input;
                    sqlParametersAssessmentAnswer[5] = new SqlParameter();
                    sqlParametersAssessmentAnswer[5].ParameterName = "@timestamp";
                    sqlParametersAssessmentAnswer[5].Value = CurrDate.ToString("dd/MM/yyyy HH:mm");
                    sqlParametersAssessmentAnswer[5].Direction = ParameterDirection.Input;
                    dh.ExecuteNonQuery("proc_AddUserAssessmentQuestionHistory", CommandType.StoredProcedure, sqlParametersAssessmentAnswer);
                }

                decimal percentage = (result.SecuredMarks * 100) / result.TotalMarks;
                if (percentage >= Convert.ToDecimal(result.PassingPercentage))
                {
                    response = true;

                    Assessments AssessmentItem = (Assessments)HttpContext.Current.Session["StartAssessment"];
                    Hashtable hashtable = new Hashtable();
                    hashtable.Add("UserName", CurrentUser.UserName);
                    hashtable.Add("ClientName", ConfigurationManager.AppSettings["ClientName"].ToString());
                    hashtable.Add("Completed Date", DateTime.Now.ToString("dd.MMM.yyy"));
                    hashtable.Add("AssessmentName", Convert.ToString(AssessmentItem.AssessmentName));
                    hashtable.Add("MarksInPercentage", percentage);
                    bool Queue = AddToEmailQueue("SendAssessmentCertificates", hashtable, CurrentUser.EmailID, null);
                }

                SqlParameter[] sqlParametersAssessmentStatus = new SqlParameter[5];
                sqlParametersAssessmentStatus[0] = new SqlParameter();
                sqlParametersAssessmentStatus[0].ParameterName = "@assessmentId";
                sqlParametersAssessmentStatus[0].Value = result.AssessmentId;
                sqlParametersAssessmentStatus[0].Direction = ParameterDirection.Input;
                sqlParametersAssessmentStatus[1] = new SqlParameter();
                sqlParametersAssessmentStatus[1].ParameterName = "@userId";
                sqlParametersAssessmentStatus[1].Value = CurrentUser.DBUserId;
                sqlParametersAssessmentStatus[1].Direction = ParameterDirection.Input;
                sqlParametersAssessmentStatus[2] = new SqlParameter();
                sqlParametersAssessmentStatus[2].ParameterName = "@marksObtained";
                sqlParametersAssessmentStatus[2].Value = result.SecuredMarks;
                sqlParametersAssessmentStatus[2].Direction = ParameterDirection.Input;
                sqlParametersAssessmentStatus[3] = new SqlParameter();
                sqlParametersAssessmentStatus[3].ParameterName = "@marksInPercentage";
                sqlParametersAssessmentStatus[3].Value = percentage;
                sqlParametersAssessmentStatus[3].Direction = ParameterDirection.Input;
                sqlParametersAssessmentStatus[4] = new SqlParameter();
                sqlParametersAssessmentStatus[4].ParameterName = "@passed";
                if (response)
                    sqlParametersAssessmentStatus[4].Value = 1;
                else
                    sqlParametersAssessmentStatus[4].Value = 0;
                sqlParametersAssessmentStatus[4].Direction = ParameterDirection.Input;
                ds = dh.ExecuteDataSet("[dbo].[proc_UpdateUserAssessment]", CommandType.StoredProcedure, sqlParametersAssessmentStatus);

                /////Assign Points to User if assessment is completed///////////
                if (response)
                {
                    /////////////Get Assessment Details///////////////                        
                    SqlParameter[] sqlParametersAssessment = new SqlParameter[1];
                    sqlParametersAssessment[0] = new SqlParameter();
                    sqlParametersAssessment[0].ParameterName = "@assessmentID";
                    sqlParametersAssessment[0].Value = result.AssessmentId;
                    sqlParametersAssessment[0].Direction = ParameterDirection.Input;
                    ds = dh.ExecuteDataSet("[dbo].[proc_GetAssessment]", CommandType.StoredProcedure, sqlParametersAssessment);

                    List<Competence> allCompetencyLevelItems = GetAllCompetenceList();

                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        int points = 0;
                        if (ds.Tables[0].Rows[0]["Points"] != null && !(ds.Tables[0].Rows[0]["Points"] is DBNull))
                        {
                            points = Convert.ToInt32(ds.Tables[0].Rows[0]["Points"]);
                        }
                        int userSkillId = Convert.ToInt32(ds.Tables[0].Rows[0]["SkillId"].ToString());
                        int competenceLevelId = Convert.ToInt32(ds.Tables[0].Rows[0]["CompetencyLevelId"].ToString());
                        List<Competence> querySkillCompetencyLevelItems = allCompetencyLevelItems.Where(c => c.CompetenceId == competenceLevelId).ToList();

                        ////////////////////Get UserPoint record///////////////////
                        SqlParameter[] sqlParametersUserPoint = new SqlParameter[3];
                        sqlParametersUserPoint[0] = new SqlParameter();
                        sqlParametersUserPoint[0].ParameterName = "@userId";
                        sqlParametersUserPoint[0].Value = CurrentUser.DBUserId;
                        sqlParametersUserPoint[0].Direction = ParameterDirection.Input;
                        sqlParametersUserPoint[1] = new SqlParameter();
                        sqlParametersUserPoint[1].ParameterName = "@skillId";
                        sqlParametersUserPoint[1].Value = userSkillId;
                        sqlParametersUserPoint[1].Direction = ParameterDirection.Input;
                        sqlParametersUserPoint[2] = new SqlParameter();
                        sqlParametersUserPoint[2].ParameterName = "@competencyId";
                        sqlParametersUserPoint[2].Value = competenceLevelId;
                        sqlParametersUserPoint[2].Direction = ParameterDirection.Input;
                        ds = dh.ExecuteDataSet("[dbo].[proc_GetUserPoint]", CommandType.StoredProcedure, sqlParametersUserPoint);

                        if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                        {
                            DataRow userPointItem = ds.Tables[0].Rows[0];
                            if (userPointItem != null)
                            {
                                int currentTrainingPoint = 0;
                                int currentAssessmentPoint = 0;
                                SqlParameter[] sqlParametersAssessmentPoint = new SqlParameter[5];
                                sqlParametersAssessmentPoint[0] = new SqlParameter();
                                sqlParametersAssessmentPoint[0].ParameterName = "@userId";
                                sqlParametersAssessmentPoint[0].Value = CurrentUser.DBUserId;
                                sqlParametersAssessmentPoint[0].Direction = ParameterDirection.Input;
                                sqlParametersAssessmentPoint[1] = new SqlParameter();
                                sqlParametersAssessmentPoint[1].ParameterName = "@skillId";
                                sqlParametersAssessmentPoint[1].Value = userSkillId;
                                sqlParametersAssessmentPoint[1].Direction = ParameterDirection.Input;
                                sqlParametersAssessmentPoint[2] = new SqlParameter();
                                sqlParametersAssessmentPoint[2].ParameterName = "@competencyId";
                                sqlParametersAssessmentPoint[2].Value = competenceLevelId;
                                sqlParametersAssessmentPoint[2].Direction = ParameterDirection.Input;
                                sqlParametersAssessmentPoint[3] = new SqlParameter();
                                sqlParametersAssessmentPoint[3].ParameterName = "@points";
                                sqlParametersAssessmentPoint[3].Direction = ParameterDirection.Input;

                                if (userPointItem["AssessmentPoints"] != null)
                                {
                                    int currentPoints = 0;
                                    if (userPointItem["AssessmentPoints"] != null && !(userPointItem["AssessmentPoints"] is DBNull))
                                    {
                                        decimal d = decimal.Parse(userPointItem["AssessmentPoints"].ToString());
                                        currentPoints = (int)d;
                                        currentPoints = currentPoints + points;
                                        sqlParametersAssessmentPoint[3].Value = currentPoints;
                                        currentAssessmentPoint = currentPoints;
                                    }
                                    else
                                    {
                                        sqlParametersAssessmentPoint[3].Value = points;
                                        currentAssessmentPoint = points;
                                    }
                                }
                                else
                                {
                                    sqlParametersAssessmentPoint[3].Value = points;
                                    currentAssessmentPoint = points;
                                }

                                if (userPointItem["TrainingPoints"] != null && !(userPointItem["TrainingPoints"] is DBNull))
                                {
                                    currentTrainingPoint = Convert.ToInt32(userPointItem["TrainingPoints"]);
                                }

                                bool ifElevate = false;
                                int competencyLevelOrder = 0;
                                int completionStatus = 0;
                                if (userPointItem["CompletionStatus"] != null && !(userPointItem["CompletionStatus"] is DBNull))
                                {
                                    if (userPointItem["CompletionStatus"].ToString().ToUpper() == "TRUE")
                                        completionStatus = 1;
                                }
                                //////////Check if current competency level is complete////////////
                                if (querySkillCompetencyLevelItems != null && querySkillCompetencyLevelItems.Count > 0)
                                {
                                    int trainingCompletionPoints = 0;
                                    int assessmentCompletionPoints = 0;
                                    trainingCompletionPoints = querySkillCompetencyLevelItems[0].TrainingCompletionPoints;

                                    assessmentCompletionPoints = querySkillCompetencyLevelItems[0].AssessmentCompletionPoints;
                                    if (currentAssessmentPoint >= assessmentCompletionPoints && currentTrainingPoint >= trainingCompletionPoints)
                                    {
                                        completionStatus = 1;
                                        competencyLevelOrder = querySkillCompetencyLevelItems[0].CompetencyLevelOrder;
                                        ifElevate = true;
                                    }
                                }
                                sqlParametersAssessmentPoint[4] = new SqlParameter();
                                sqlParametersAssessmentPoint[4].ParameterName = "@completionStatus";
                                sqlParametersAssessmentPoint[4].Value = completionStatus;
                                sqlParametersAssessmentPoint[4].Direction = ParameterDirection.Input;
                                dh.ExecuteNonQuery("[dbo].[proc_UpdateAssessmentPoint]", CommandType.StoredProcedure, sqlParametersAssessmentPoint);

                                /////Elevate the user to next competency level/////
                                if (ifElevate)
                                {
                                    List<Competence> skillCompItems = allCompetencyLevelItems.Where(c => c.CompetencyLevelOrder == (competencyLevelOrder + 1) && c.SkillId == userSkillId).ToList();
                                    if (skillCompItems != null && skillCompItems.Count > 0)
                                    {
                                        ///////Get record for the skill from UserSkills list/////////
                                        SqlParameter[] sqlParametersUserCompetencyLevel = new SqlParameter[3];
                                        sqlParametersUserCompetencyLevel[0] = new SqlParameter();
                                        sqlParametersUserCompetencyLevel[0].ParameterName = "@userId";
                                        sqlParametersUserCompetencyLevel[0].Value = CurrentUser.DBUserId;
                                        sqlParametersUserCompetencyLevel[0].Direction = ParameterDirection.Input;
                                        sqlParametersUserCompetencyLevel[1] = new SqlParameter();
                                        sqlParametersUserCompetencyLevel[1].ParameterName = "@skillId";
                                        sqlParametersUserCompetencyLevel[1].Value = userSkillId;
                                        sqlParametersUserCompetencyLevel[1].Direction = ParameterDirection.Input;
                                        sqlParametersUserCompetencyLevel[2] = new SqlParameter();
                                        sqlParametersUserCompetencyLevel[2].ParameterName = "@competencyId";
                                        sqlParametersUserCompetencyLevel[2].Value = skillCompItems[0].CompetenceId.ToString();
                                        sqlParametersUserCompetencyLevel[2].Direction = ParameterDirection.Input;
                                        dh.ExecuteNonQuery("[dbo].[proc_UpdateUserCompetencyLevel]", CommandType.StoredProcedure, sqlParametersUserCompetencyLevel);

                                        ////////Assign training and assessment for the elevated competency level///////////////
                                        AddSkillBasedTrainingAssessment(skillCompItems[0].CompetenceId.ToString(), userSkillId, Convert.ToInt32(CurrentUser.DBUserId));

                                        ///////Reset UserPoints record for elevated competency level/////////////
                                        SqlParameter[] sqlParametersAddUserPoint = new SqlParameter[3];
                                        sqlParametersAddUserPoint[0] = new SqlParameter();
                                        sqlParametersAddUserPoint[0].ParameterName = "@userId";
                                        sqlParametersAddUserPoint[0].Value = CurrentUser.DBUserId;
                                        sqlParametersAddUserPoint[0].Direction = ParameterDirection.Input;
                                        sqlParametersAddUserPoint[1] = new SqlParameter();
                                        sqlParametersAddUserPoint[1].ParameterName = "@skillId";
                                        sqlParametersAddUserPoint[1].Value = userSkillId;
                                        sqlParametersAddUserPoint[1].Direction = ParameterDirection.Input;
                                        sqlParametersAddUserPoint[2] = new SqlParameter();
                                        sqlParametersAddUserPoint[2].ParameterName = "@competencyId";
                                        sqlParametersAddUserPoint[2].Value = skillCompItems[0].CompetenceId.ToString();
                                        sqlParametersAddUserPoint[2].Direction = ParameterDirection.Input;
                                        dh.ExecuteNonQuery("[dbo].[proc_AddUserPoint]", CommandType.StoredProcedure, sqlParametersAddUserPoint);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AssessmentResult", ex.Message, ex.StackTrace));
            }
            return response;
        }
        private bool AddToEmailQueue(string templateCode, Hashtable dynamicKeyValues, string RecipientTo, string recipientCc)
        {
            string emailSub = string.Empty;
            string emailBody = string.Empty;
            DataHelper dh = new DataHelper(strConnectionString);
            DataSet ds = new DataSet();
            SqlParameter[] parameters =
            {
                new SqlParameter("@TemplateCode", SqlDbType.NVarChar) { Value = templateCode, Direction = ParameterDirection.Input  }
            };


            ds = dh.ExecuteDataSet("[dbo].[proc_GetEmailTemplate]", CommandType.StoredProcedure, parameters);

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                try
                {
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        emailSub = Convert.ToString(item["EmailSubject"]);
                        emailBody = Convert.ToString(item["EmailBody"]);

                        foreach (string key in dynamicKeyValues.Keys)
                        {
                            emailSub = emailSub.Replace("[##" + key + "##]", Convert.ToString(dynamicKeyValues[key]));
                            emailBody = emailBody.Replace("[##" + key + "##]", Convert.ToString(dynamicKeyValues[key]));
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SqlSvrDAL, AddToEmailQueue", ex.Message, ex.StackTrace));

                }
            }

            bool status = false;

            string clientName = ConfigurationManager.AppSettings["ClientName"].ToString();

            SendMailRequest objtb = new SendMailRequest();
            objtb.To = RecipientTo;
            objtb.Cc = recipientCc;
            objtb.SenderEmailId = "no-reply@hcl.com";
            objtb.SenderName = "HCL Academy";
            if (!string.IsNullOrEmpty(clientName))
            {
                objtb.SenderName = clientName + " Academy";
            }
            objtb.Subject = emailSub;
            objtb.Body = emailBody;
            Task.Factory.StartNew(() => EmailHelper.SendEmail(objtb));
            status = true;
            return status;
        }

        public List<Users> GetUsers()
        {
            List<Users> lstUsers = new List<Users>();
            DataSet ds = new DataSet();
            DataView dv = new DataView();
            DataHelper dh = new DataHelper(strConnectionString);

            try
            {
                ds = dh.ExecuteDataSet("[dbo].[proc_GetUsers]", CommandType.StoredProcedure);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllProjects", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }

            DataTable dt = dv.ToTable();

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    Users item = new Users();
                    item.userID = Convert.ToInt32(row["UserID"]);
                    item.userName = row["UserName"].ToString();
                    item.projectName = row["ProjectName"].ToString();
                    lstUsers.Add(item);
                }
            }

            return lstUsers;
        }

        public void UpdateProjectData(AssignUser objUserOnboard)
        {
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@UserId",SqlDbType.Int),
                    new SqlParameter("@ProjectId",SqlDbType.Int ),
                    new SqlParameter("@ErrorNumber",SqlDbType.Int),
                    new SqlParameter("@ErrorMessage",SqlDbType.VarChar),
                };
                parameters[0].Value = objUserOnboard.selectedUser;
                parameters[1].Value = objUserOnboard.selectedProject;
                parameters[2].Direction = ParameterDirection.Output;
                parameters[3].Size = 4000;
                parameters[3].Direction = ParameterDirection.Output;
                dh.ExecuteNonQuery("[dbo].[proc_UpdateProjectData]", CommandType.StoredProcedure, parameters);

                if (dh.Cmd != null && dh.Cmd.Parameters["@ErrorNumber"].Value != DBNull.Value && dh.Cmd.Parameters["@ErrorMessage"].Value != DBNull.Value)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SqlSvrDAL, UpdateProject", dh.Cmd.Parameters["@ErrorNumber"].Value.ToString(), dh.Cmd.Parameters["@ErrorMessage"].Value.ToString()));
                }
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
        }

        public List<WikiPolicies> GetAllWikiPolicies()
        {
            List<WikiPolicies> wikiPol = new List<WikiPolicies>();
            return wikiPol;
        }

        public List<Skills> GetSkills()
        {
            List<Skills> allSkills = new List<Skills>();
            List<Skill> skills = GetAllSkills();
            List<Competence> competencies = GetAllCompetenceList();
            List<SkillCompetencies> trainings = new List<SkillCompetencies>();
            trainings = GetSkillCompetencyTraingings();
            if (competencies != null && competencies.Count() > 0)
            {
                foreach (Skill skill in skills)
                {
                    Skills objSkill = new Skills();
                    objSkill.skillName = skill.SkillName;

                    List<Competence> competenciesBySkill = competencies.Where(i => i.SkillId == skill.SkillId).ToList();
                    if (competenciesBySkill != null && competenciesBySkill.Count() > 0)
                    {
                        SkillCompetencies objCompetencyDetail = null;
                        objSkill.competences = new List<SkillCompetencies>();
                        foreach (Competence competency in competenciesBySkill)
                        {
                            objCompetencyDetail = new SkillCompetencies();
                            List<SkillCompetencies> SelectedTraining = trainings.Where(m => m.SkillId == skill.SkillId && m.CompetenceId == competency.CompetenceId).ToList();
                            var ulHTML = new TagBuilder("ul");
                            StringBuilder output = new StringBuilder();
                            string trainingDescription = "";
                            foreach (SkillCompetencies lstitem in SelectedTraining)
                            {

                                var liHTML = new TagBuilder("li");
                                liHTML.MergeAttribute("style", "list-style-type:disc;margin-left:15px");
                                liHTML.SetInnerText(lstitem.TrainingName);
                                output.Append(liHTML.ToString());
                                trainingDescription += lstitem.TrainingDescription + System.Environment.NewLine;
                            }
                            ulHTML.InnerHtml = output.ToString();
                            objCompetencyDetail.CompetenceId = competency.CompetenceId;
                            objCompetencyDetail.CompetenceName = competency.CompetenceName;
                            objCompetencyDetail.Description = competency.Description;
                            objCompetencyDetail.SkillId = competency.SkillId;
                            objCompetencyDetail.TrainingName = ulHTML.ToString();
                            objCompetencyDetail.TrainingDescription = trainingDescription;
                            objSkill.competences.Add(objCompetencyDetail);
                        }
                    }
                    allSkills.Add(objSkill);
                }
            }
            return allSkills;
        }

        public List<Result> Search(string keyword)
        {
            List<Result> lstResult = null;
            return lstResult;
        }

        public HeatMapProjectDetail GetHeatMapProjectDetailByProjectID(int projectID)
        {
            HeatMapProjectDetail heatMapProjectDetail = new HeatMapProjectDetail();
            heatMapProjectDetail.ID = projectID;
            heatMapProjectDetail.CompetencyLevel = "Novice";
            String ProjectName = String.Empty;
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                ProjectName = dh.ExecuteScalar("[dbo].[proc_GetProjectById]", CommandType.StoredProcedure, new SqlParameter("@ProjectID", projectID)).ToString();
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SqlSvrDAL, GetHeatMapProjectDetailByProjectID", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            if (ProjectName != String.Empty)
            {
                heatMapProjectDetail.ProjectName = ProjectName;
            }
            return heatMapProjectDetail;
        }

        public List<SiteMenu> GetMenu()
        {
            List<SiteMenu> siteMenu = null;
            UserManager userManager = new UserManager();
            List<int> userRoleList = new List<int>();
            List<SiteMenu> roleBasedsiteMenu = new List<SiteMenu>();
            try
            {
                DataSet dsRoles = new DataSet();
                DataView dvRoles = new DataView();
                DataHelper dhRoles = new DataHelper(strConnectionString);
                SqlParameter[] sqlParameters = new SqlParameter[1];
                sqlParameters[0] = new SqlParameter();
                sqlParameters[0].ParameterName = "@UserID";
                sqlParameters[0].Value = CurrentUser.DBUserId;
                sqlParameters[0].Direction = ParameterDirection.Input;
                dsRoles = dhRoles.ExecuteDataSet("[dbo].[proc_GetOnBoardedUserRoles]", CommandType.StoredProcedure, sqlParameters);
                if (dsRoles.Tables.Count > 0)
                    dvRoles = new DataView(dsRoles.Tables[0]);
                DataTable dtRoles = dvRoles.ToTable();
                if (dtRoles != null && dtRoles.Rows.Count > 0)
                {
                    foreach (DataRow row in dtRoles.Rows)
                    {
                        var userRoleValue = row["RoleName"].ToString();
                        int userRoleId = Convert.ToInt32(row["RoleID"]);
                        userRoleList.Add(userRoleId);
                    }
                }
                System.Web.HttpContext.Current.Session["UserRole"] = userRoleList;
                siteMenu = GetMenuList();
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetMenu", ex.Message, ex.StackTrace));
            }

            return siteMenu;
        }
        public int GetUserId(string emailId)
        {
            DataSet ds = new DataSet();
            DataHelper dh = new DataHelper(strConnectionString);
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter();
            sqlParameters[0].ParameterName = "@emailAddress";
            sqlParameters[0].Value = emailId;
            sqlParameters[0].Direction = ParameterDirection.Input;
            object userId = dh.ExecuteScalar("[dbo].[proc_GetUserId]", CommandType.StoredProcedure, sqlParameters);
            return Convert.ToInt32(userId);
        }
        public List<SiteMenu> GetMenuList()
        {
            List<SiteMenu> siteMenu = null;
            List<int> userRoleList = new List<int>();
            List<SiteMenu> roleBasedsiteMenu = new List<SiteMenu>();
            List<UserRole> userRoles = new List<UserRole>();
            DataHelper dhRoles = new DataHelper(strConnectionString);
            try
            {
                DataSet dsRoles = new DataSet();
                DataView dvRoles = new DataView();
                DataSet dsMenu = new DataSet();
                DataView dvMenu = new DataView();
                dsRoles = dhRoles.ExecuteDataSet("[dbo].[proc_GetAllRoles]", CommandType.StoredProcedure);
                if (dsRoles.Tables.Count > 0)
                    dvRoles = new DataView(dsRoles.Tables[0]);
                dsMenu = dhRoles.ExecuteDataSet("[dbo].[proc_GetMenu]", CommandType.StoredProcedure);
                if (dsMenu.Tables.Count > 0)
                    dvMenu = new DataView(dsMenu.Tables[0]);


                DataTable dtRoles = dvRoles.ToTable();
                if (dtRoles != null && dtRoles.Rows.Count > 0)
                {
                    foreach (DataRow row in dtRoles.Rows)
                    {
                        UserRole role = new UserRole();
                        role.RoleId = Convert.ToInt32(row["RoleId"]);
                        role.RoleName = row["RoleName"].ToString();
                        userRoles.Add(role);
                        userRoleList.Add(role.RoleId);
                    }
                    System.Web.HttpContext.Current.Session["UserRole"] = userRoleList;
                }
                if (siteMenu == null)
                {
                    siteMenu = new List<SiteMenu>();
                    DataTable dtMenu = dvMenu.ToTable();

                    if (dtMenu != null && dtMenu.Rows.Count > 0)
                    {
                        foreach (DataRow row in dtMenu.Rows)
                        {
                            SiteMenu item = new SiteMenu();
                            item.ItemId = Int32.Parse(row["ID"].ToString());
                            item.ItemName = row["Title"].ToString();
                            item.ItemOrdering = Convert.ToInt32(row["Ordering"].ToString());
                            item.ParentItemId = Convert.ToInt32(row["ParentMenu"].ToString());
                            item.ItemTarget = row["Target"].ToString();
                            item.ItemHidden = row["Hidden"].ToString();
                            item.ItemURL = row["ControllerView"].ToString();
                            item.UserRole = new List<UserRole>();
                            foreach (var role in userRoles)
                            {
                                UserRole userRole = new UserRole();
                                userRole.RoleId = role.RoleId;
                                userRole.RoleName = role.RoleName;
                                item.UserRole.Add(userRole);
                            }
                            siteMenu.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dhRoles != null)
                {
                    if (dhRoles.DataConn != null)
                    {
                        dhRoles.DataConn.Close();
                    }
                }
            }
            return siteMenu;
        }

        public UserManager GetCurrentUserCompleteUserProfile()
        {
            UserManager user = null;
            return user;
        }

        public List<Banners> GetBanners()
        {
            List<Banners> bannersList = new List<Banners>();
            return bannersList;
        }

        public List<Skill> GetAllSkills()
        {
            List<Skill> skills = null;
            DataHelper dh = new DataHelper(strConnectionString);
            if (skills == null)
            {
                skills = new List<Skill>();
                try
                {
                    DataSet ds = new DataSet();
                    DataView dv = new DataView();
                    ds = dh.ExecuteDataSet("[dbo].[proc_GetAllSkills]", CommandType.StoredProcedure);
                    if (ds.Tables.Count > 0)
                        dv = new DataView(ds.Tables[0]);
                    DataTable dt = dv.ToTable();

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            Skill item = new Skill();
                            item.SkillId = Int32.Parse(row["ID"].ToString());
                            item.SkillName = row["Title"].ToString();
                            //item.Skills = ??? 
                            skills.Add(item);
                        }
                    }

                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
                }
                finally
                {
                    if (dh != null)
                    {
                        if (dh.DataConn != null)
                        {
                            dh.DataConn.Close();
                        }
                    }
                }
            }
            return skills;
        }

        public TrainingReport GetTrainingsReport(string skillid, string competencyid)
        {
            TrainingReport trainingReport = new TrainingReport();
            try
            {
                List<UserDetails> usersList = new List<UserDetails>();
                trainingReport.userDetails = GetUserDetails(Convert.ToInt32(skillid), Convert.ToInt32(competencyid));
                trainingReport.counts = GetTrainingCounts(trainingReport.userDetails);
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SqlSvrDAL, GetTrainingsReport", ex.Message, ex.StackTrace));

            }
            return trainingReport;
        }

        public List<RSSFeed> GetRSSFeeds()
        {
            List<RSSFeed> rSSFeeds = new List<RSSFeed>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();

                ds = dh.ExecuteDataSet("[dbo].[proc_GetRssFeeds]", CommandType.StoredProcedure);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow item in dt.Rows)
                    {
                        XmlDocument rssXmlDoc = new XmlDocument();

                        rssXmlDoc.Load(Convert.ToString(item["RssFeedUrl"]));

                        // Parse the Items in the RSS file
                        XmlNodeList rssNodes = rssXmlDoc.SelectNodes(Convert.ToString(item["ItemNodePath"]));

                        // Iterate through the items in the RSS file
                        foreach (XmlNode rssNode in rssNodes)
                        {
                            RSSFeed rs = new RSSFeed();
                            XmlNode rssSubNode = rssNode.SelectSingleNode(Convert.ToString(item["TitleNodePath"])); //for Node Title text
                            rs.TitleNode = rssSubNode != null ? rssSubNode.InnerText : "";


                            rssSubNode = rssNode.SelectSingleNode(Convert.ToString(item["hrfTitleNodePath"]));  //for Node hyperlink on Node Title text
                            rs.LinkNode = rssSubNode != null ? rssSubNode.InnerText : "";

                            rssSubNode = rssNode.SelectSingleNode(Convert.ToString(item["DescNodePath"])); //for Node Description text
                            rs.DescriptionNode = rssSubNode != null ? rssSubNode.InnerText : "";

                            string TrimedDespt = Utilities.Truncate(HtmlToPlainText(Convert.ToString(item["DescNodePath"])), 25);
                            rssSubNode = rssNode.SelectSingleNode(TrimedDespt); //for Node Description text
                            rs.TrimedDescription = rssSubNode != null ? rssSubNode.InnerText : "";

                            rssSubNode = rssNode.SelectSingleNode(Convert.ToString(item["PubDateNodePath"])); //for Publish date value
                            rs.PubDateNode = rssSubNode != null ? rssSubNode.InnerText : "";

                            string strTitle = Convert.ToString(item["Title"]);    // for RssFeed Title
                            rs.Title = strTitle != null ? strTitle : "";
                            rSSFeeds.Add(rs);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return rSSFeeds;
        }
        private static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);
            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);
            return text;
        }

        public List<News> GetNews(string NoImagePath)
        {
            return null;
        }

        public List<AcademyEvent> GetEvents()
        {
            return null;
        }

        public List<TrainingPlan> GetTrainingPlans(int id)
        {
            List<TrainingPlan> trainingPlans = new List<TrainingPlan>();
            return trainingPlans;
        }

        public List<UserTraining> GetTrainingForUser(int userId, bool OnlyOnBoardedTraining = false)
        {
            List<UserTraining> trainings = new List<UserTraining>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@userID", userId) };
                ds = dh.ExecuteDataSet("[dbo].[proc_GetTrainingForUser]", CommandType.StoredProcedure, parameters);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    UserTraining objTraining = null;
                    foreach (DataRow item in dt.Rows)
                    {
                        objTraining = new UserTraining();

                        if (item["CompletedDate"] != null && !(item["CompletedDate"] is DBNull))
                            objTraining.CompletedDate = Convert.ToString(item["CompletedDate"]);
                        else
                            objTraining.CompletedDate = "";
                        if (item["IsIncludeOnBoarding"] != null && !(item["IsIncludeOnBoarding"] is DBNull))
                            objTraining.IsIncludeOnBoarding = Convert.ToBoolean(item["IsIncludeOnBoarding"]);
                        if (item["IsMandatory"] != null && !(item["IsMandatory"] is DBNull))
                            objTraining.IsMandatory = Convert.ToBoolean(item["IsMandatory"]);
                        if (item["IsTrainingActive"] != null && !(item["IsTrainingActive"] is DBNull))
                            objTraining.IsTrainingActive = Convert.ToBoolean(item["IsTrainingActive"]);
                        if (item["IsTrainingCompleted"] != null && !(item["IsTrainingCompleted"] is DBNull))
                            objTraining.IsTrainingCompleted = Convert.ToBoolean(item["IsTrainingCompleted"]);
                        if (item["LastDayCompletion"] != null && !(item["LastDayCompletion"] is DBNull))
                            objTraining.LastDayCompletion = Convert.ToDateTime(item["LastDayCompletion"].ToString()).ToShortDateString();
                        if (item["Skill"] != null && !(item["Skill"] is DBNull))
                            objTraining.SkillName = item["Skill"] != null ? item["Skill"].ToString() : "";
                        if (item["SkillID"] != null && !(item["SkillID"] is DBNull))
                            objTraining.SkillId= item["SkillID"] != null ? (Convert.ToInt32(item["SkillID"])) : 0;
                        if (item["Training"] != null && !(item["Training"] is DBNull))
                            objTraining.TrainingName = item["Training"] != null ? ((item["Training"]).ToString()) : "";
                        if (item["TrainingID"] != null && !(item["TrainingID"] is DBNull))
                            objTraining.TrainingId= item["TrainingID"] != null ? (Convert.ToInt32(item["TrainingID"])) : 0;

                        if (objTraining.IsTrainingCompleted)
                        {
                            objTraining.StatusColor = AppConstant.Green;
                            objTraining.ItemStatus = AppConstant.Completed;
                        }
                        else if (objTraining.IsTrainingCompleted == false && Convert.ToDateTime(objTraining.LastDayCompletion) < DateTime.Now)
                        {
                            objTraining.StatusColor = AppConstant.Red;
                            objTraining.ItemStatus = AppConstant.OverDue;
                        }
                        else
                        {
                            objTraining.StatusColor = AppConstant.Amber;
                            objTraining.ItemStatus = AppConstant.InProgress;
                        }
                        trainings.Add(objTraining);
                    }
                }


            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return trainings;
        }

        public List<OnBoarding> GetBoardingData()
        {
            List<OnBoarding> onBoardings = new List<OnBoarding>();
            UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet dsUserTrainings = new DataSet();
                DataView dvUserTrainings = new DataView();
                DataSet dsRoleTrainings = new DataSet();
                DataView dvRoleTrainings = new DataView();
                SqlParameter[] sqlParameters = new SqlParameter[2];

                sqlParameters[0] = new SqlParameter();
                sqlParameters[0].ParameterName = "@userID";
                sqlParameters[0].Value = user.DBUserId;
                sqlParameters[0].Direction = ParameterDirection.Input;

                sqlParameters[1] = new SqlParameter();
                sqlParameters[1].ParameterName = "@roleBasedTraining";
                sqlParameters[1].Value = 0;
                sqlParameters[1].Direction = ParameterDirection.Input;
                dsUserTrainings = dh.ExecuteDataSet("[dbo].[proc_GetOnBoardingData]", CommandType.StoredProcedure, sqlParameters);
                if (dsUserTrainings.Tables.Count > 0)
                    dvUserTrainings = new DataView(dsUserTrainings.Tables[0]);

                SqlParameter[] sqlParametersRoleTraining = new SqlParameter[2];

                sqlParametersRoleTraining[0] = new SqlParameter();
                sqlParametersRoleTraining[0].ParameterName = "@userID";
                sqlParametersRoleTraining[0].Value = user.DBUserId;
                sqlParametersRoleTraining[0].Direction = ParameterDirection.Input;

                sqlParametersRoleTraining[1] = new SqlParameter();
                sqlParametersRoleTraining[1].ParameterName = "@roleBasedTraining";
                sqlParametersRoleTraining[1].Value = 1;
                sqlParametersRoleTraining[1].Direction = ParameterDirection.Input;

                dsRoleTrainings = dh.ExecuteDataSet("[dbo].[proc_GetOnBoardingData]", CommandType.StoredProcedure, sqlParametersRoleTraining);
                if (dsRoleTrainings.Tables.Count > 0)
                    dvRoleTrainings = new DataView(dsRoleTrainings.Tables[0]);

                DataTable dtUserTrainings = dvUserTrainings.ToTable();
                DataTable dtRoleTrainings = dvRoleTrainings.ToTable();
                bool trainingStatus = false;
                if (dtUserTrainings != null && dtUserTrainings.Rows.Count > 0)
                {
                    foreach (DataRow row in dtUserTrainings.Rows)
                    {
                        trainingStatus = false;
                        if (!String.IsNullOrEmpty(row["IsTrainingCompleted"].ToString()) && !(row["IsTrainingCompleted"] is DBNull))
                        {
                            if (row["IsTrainingCompleted"].ToString().ToUpper() == "TRUE")
                            {
                                trainingStatus = true;
                            }
                        }

                        OnBoarding item = new OnBoarding();
                        item.BoardingItemId = Convert.ToInt32(row["ID"]);
                        if (!String.IsNullOrEmpty(row["LastDayCompletion"].ToString()))
                            item.BoardingStatus = Utilities.GetOnBoardingStatus(trainingStatus, 0, Convert.ToDateTime(row["LastDayCompletion"]));
                        item.BoardingType = OnboardingItemType.Training;
                        if (!String.IsNullOrEmpty(row["Training"].ToString()))
                        {
                            item.BoardingItemName = row["Training"].ToString();
                        }
                        item.BoardIngTrainingId = Convert.ToInt32(row["TrainingID"]);
                        onBoardings.Add(item);
                    }
                }
                if (dtRoleTrainings != null && dtRoleTrainings.Rows.Count > 0)
                {
                    foreach (DataRow row in dtRoleTrainings.Rows)
                    {
                        OnBoarding item = new OnBoarding();
                        item.BoardingItemId = Convert.ToInt32(row["ID"]);
                        if (!String.IsNullOrEmpty(row["LastDayCompletion"].ToString()))
                        {
                            item.BoardingStatus = Utilities.GetOnBoardingStatus(trainingStatus, 0, Convert.ToDateTime(row["LastDayCompletion"]));
                        }
                        if (!String.IsNullOrEmpty(row["Training"].ToString()))
                        {
                            item.BoardingItemName = row["Training"].ToString();
                        }
                        item.BoardingType = OnboardingItemType.RoleTraining;
                        item.BoardIngTrainingId = Convert.ToInt32(row["TrainingID"]);
                        onBoardings.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return onBoardings;
        }


        public List<UserSkill> GetUserSkillsOfCurrentUser()
        {
            List<UserSkill> lstSkills = new List<UserSkill>();
            UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();

                ds = dh.ExecuteDataSet("[dbo].[proc_GetUserSkillsForCurrentUser]", CommandType.StoredProcedure, new SqlParameter("@UserID", users.DBUserId));
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        UserSkill item = new UserSkill();
                        item.Id = Int32.Parse(row["ID"].ToString());
                        item.Competence = row["CompetencyLevel"].ToString().ToUpper();
                        item.SkillId = Int32.Parse(row["SkillID"].ToString());
                        item.Skill = row["SkillName"].ToString();
                        lstSkills.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return lstSkills;
        }
        public List<Competence> GetAllCompetenceList()
        {
            DataSet ds = new DataSet();
            DataHelper dh = new DataHelper(strConnectionString);
            List<Competence> competenceList = new List<Competence>();
            try
            {
                ds = dh.ExecuteDataSet("[dbo].[proc_GetAllCompetencies]", CommandType.StoredProcedure);
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow row in ds.Tables[0].Rows)
                        {
                            Competence item = new Competence();
                            if (row["ID"] != null && !(row["ID"] is DBNull))
                                item.CompetenceId = Int32.Parse(row["ID"].ToString());
                            if (row["CompetenceName"] != null && !(row["CompetenceName"] is DBNull))
                                item.CompetenceName = row["CompetenceName"].ToString();
                            if (row["SkillId"] != null && !(row["SkillId"] is DBNull))
                                item.SkillId = Int32.Parse(row["SkillId"].ToString());
                            if (row["SkillName"] != null && !(row["SkillName"] is DBNull))
                                item.SkillName = row["SkillName"].ToString();
                            if (row["Description"] != null && !(row["Description"] is DBNull))
                                item.Description = row["Description"].ToString();
                            if (row["CompetencyLevelOrder"] != null && !(row["CompetencyLevelOrder"] is DBNull))
                                item.CompetencyLevelOrder = Convert.ToInt32(row["CompetencyLevelOrder"].ToString());
                            competenceList.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return competenceList;
        }

        public List<ProjectSkill> GetAllProjectSkills()
        {
            List<ProjectSkill> projectSkills = new List<ProjectSkill>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();

                ds = dh.ExecuteDataSet("[dbo].[proc_GetAllProjectSkills]", CommandType.StoredProcedure);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        ProjectSkill item = new ProjectSkill();
                        item.ItemId = Convert.ToInt32(row["ID"]);
                        item.Project = row["Project"].ToString();
                        item.ProjectId = Convert.ToInt32(row["ProjectID"].ToString());
                        item.Skill = row["Skill"].ToString();
                        item.SkillId = Convert.ToInt32(row["SkillID"].ToString());
                        projectSkills.Add(item);
                    }
                }

            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return projectSkills;
        }

        public void AddProjectSkill(int ProjectID, int SkillID)
        {
            DataHelper dh = new DataHelper(strConnectionString);
            SqlParameter[] parameters =
            {
                new SqlParameter("@ProjectId",SqlDbType.Int ) { Value = ProjectID},
                new SqlParameter("@SkillId",SqlDbType.Int ) { Value = SkillID}
            };
            try
            {
                dh.ExecuteNonQuery("[dbo].[proc_AddProjectSkill]", CommandType.StoredProcedure, parameters);
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
        }

        public List<ProjectSkillResource> GetAllProjectSkillResources()
        {
            List<ProjectSkillResource> projectSkillResources = new List<ProjectSkillResource>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetAllProjectSkillResources]", CommandType.StoredProcedure);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        ProjectSkillResource item = new ProjectSkillResource();
                        item.ProjectName = row["Project"].ToString();
                        item.ProjectId = Convert.ToInt32(row["ProjectId"]);
                        item.Skill = row["Skill"].ToString();
                        item.SkillId = Convert.ToInt32(row["SkillId"].ToString());
                        item.CompetencyLevel = row["CompetencyLevel"].ToString();
                        item.CompetencyLevelId = Convert.ToInt32(row["CompetencyLevelId"].ToString());
                        item.ExpectedResourceCount = Convert.ToString(row["ExpectedResourceCount"]);
                        item.AvailableResourceCount = Convert.ToString(row["AvailableResourceCount"]);
                        projectSkillResources.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return projectSkillResources;
        }

        public void AddProjectSkillResource(int ProjectID, ProjectSkillResource skillResource)
        {
            DataHelper dh = new DataHelper(strConnectionString);
            SqlParameter[] parameters =
           {
                new SqlParameter("@ProjectId", SqlDbType.Int) { Value = ProjectID },
                new SqlParameter("@SkillId", SqlDbType.Int) { Value = skillResource.SkillId  },
                new SqlParameter("@CompetencyLevelId", SqlDbType.Int) { Value = skillResource.CompetencyLevelId },
                new SqlParameter("@ExpectedResourceCount", SqlDbType.Int) { Value = Int32.Parse(skillResource.ExpectedResourceCount) },
                new SqlParameter("@AvailableResourceCount", SqlDbType.Int) { Value = Int32.Parse(skillResource.AvailableResourceCount) }

            };
            try
            {
                dh.ExecuteNonQuery("[dbo].[proc_AddProjectSkillResource]", CommandType.StoredProcedure, parameters);
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
        }
        public List<ProjectSkillResource> GetAllProjectSkillResourcesByProjectID(int ProjectID)
        {
            List<ProjectSkillResource> projectSkillResources = new List<ProjectSkillResource>();
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                DataHelper dh = new DataHelper(strConnectionString);

                SqlParameter[] parameters =
                {
                    new SqlParameter("@ProjectID",SqlDbType.Int),
                    new SqlParameter("@ProjectName",SqlDbType.VarChar)
                };
                parameters[0].Value = ProjectID;
                parameters[1].Size = 255;
                parameters[1].Direction = ParameterDirection.Output;
                try
                {
                    ds = dh.ExecuteDataSet("[dbo].[proc_GetAvailableSkillResourceCountByProjectId]", CommandType.StoredProcedure, parameters);
                    if (ds.Tables.Count > 0)
                        dv = new DataView(ds.Tables[0]);
                }
                finally
                {
                    if (dh != null)
                    {
                        if (dh.DataConn != null)
                        {
                            dh.DataConn.Close();
                        }
                    }
                }

                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        ProjectSkillResource item = new ProjectSkillResource();
                        item.ProjectName = Convert.ToString(dh.Cmd.Parameters["@ProjectName"].Value);
                        item.ProjectId = Convert.ToInt32(row["ProjectID"]);
                        item.Skill = row["SkillName"].ToString();
                        item.SkillId = Convert.ToInt32(row["SkillId"].ToString());
                        item.CompetencyLevel = row["CompetencyLevel"].ToString();
                        item.CompetencyLevelId = Convert.ToInt32(row["CompetencyLevelId"].ToString());
                        item.ExpectedResourceCount = Convert.ToString(row["ExpectedResourceCount"]);
                        item.AvailableResourceCount = Convert.ToString(row["AvailableResourceCount"]);
                        projectSkillResources.Add(item);
                    }
                }

            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            return projectSkillResources;
        }

        public List<OnBoarding> GetBoardingDataFromOnboarding(ref bool sendEmail)
        {
            List<OnBoarding> boardingList = new List<OnBoarding>();
            UserManager userManager = new UserManager();
            userManager = (UserManager)HttpContext.Current.Session["CurrentUser"];
            string userName = String.Empty;
            var showDefaultTraining = "NO";
            var showSkillBasedTraining = "NO";
            var showRoleBasedTraining = "NO";
            var showAssessments = "NO";

            try
            {
                DataSet dsConfig = new DataSet();
                DataView dvConfig = new DataView();
                DataHelper dh = new DataHelper(strConnectionString);
                dsConfig = dh.ExecuteDataSet("[dbo].[proc_GetConfigItems]", CommandType.StoredProcedure);
                if (dsConfig.Tables.Count > 0)
                    dvConfig = new DataView(dsConfig.Tables[0]);

                DataTable dt = dvConfig.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string Title = String.Empty;
                        string Value = String.Empty;
                        Title = row["Title"].ToString();
                        Value = row["Value"].ToString();
                        if (Title == "ShowUserAssessments")
                        {
                            showAssessments = Value.ToUpper().ToString();
                        }
                        else if (Title == "ShowDefaultTraining")
                        {
                            showDefaultTraining = Value.ToUpper().ToString();
                        }
                        else if (Title == "ShowSkillBasedTraining")
                        {
                            showSkillBasedTraining = Value.ToUpper().ToString();
                        }
                        else if (Title == "ShowRoleBasedTraining")
                        {
                            showRoleBasedTraining = Value.ToUpper().ToString();
                        }
                    }
                }

                DataSet dsOnBoarding = new DataSet();
                DataView dvOnBoarding = new DataView();

                dsOnBoarding = dh.ExecuteDataSet("dbo.proc_GetOnBoardingDataForUser", CommandType.StoredProcedure, new SqlParameter("@userId", userManager.DBUserId));
                if (dsOnBoarding.Tables.Count > 0)
                {
                    dvOnBoarding = new DataView(dsOnBoarding.Tables[0]);
                }

                DataTable dtOnBoarding = new DataTable();
                dtOnBoarding = dvOnBoarding.ToTable();
                foreach (DataRow row in dtOnBoarding.Rows)
                {
                    if (row["SendEmail"] != null)
                    {
                        if (!String.IsNullOrEmpty(row["SendEmail"].ToString()))
                            sendEmail = Convert.ToBoolean(row["SendEmail"]) == true ? true : false;
                    }


                    int locId = 0;
                    string locValue = "";
                    if (row["GEOID"] != null)
                    {
                        if (!String.IsNullOrEmpty(row["GEOID"].ToString()))
                            locId = Convert.ToInt32(row["GEOID"]);
                    }
                    if (row["GEOID"] != null)
                    {
                        if (!String.IsNullOrEmpty(row["GEO"].ToString()))
                            locValue = row["GEO"].ToString();
                    }
                    var checkList = GetOnBoardingCheckList(locValue, locId);
                    DataSet dsUser = new DataSet();
                    DataView dvUser = new DataView();
                    dsUser = dh.ExecuteDataSet("[dbo].[proc_GetUserCheckLists]", CommandType.StoredProcedure, new SqlParameter("@userId", CurrentUser.DBUserId));
                    dvUser = new DataView(dsUser.Tables[0]);
                    DataTable dtUsers = new DataTable();
                    dtUsers = dvUser.ToTable();
                    List<UserCheckList> lstUserCheckList = new List<UserCheckList>();
                    if (dtUsers != null && dtUsers.Rows.Count > 0)
                    {
                        foreach (DataRow data in dtUsers.Rows)
                        {
                            UserCheckList item = new UserCheckList();
                            item.Id = Convert.ToInt32(data["ID"].ToString());
                            if (data["CheckList"] != null)
                            {
                                if (!String.IsNullOrEmpty(data["CheckList"].ToString()))
                                    item.CheckList = data["CheckList"].ToString();
                            }
                            if (data["CheckListStatus"] != null)
                            {
                                if (!String.IsNullOrEmpty(data["CheckListStatus"].ToString()))
                                    item.CheckListStatus = data["CheckListStatus"].ToString();
                            }
                            lstUserCheckList.Add(item);
                        }
                    }
                    for (int i = 0; i < lstUserCheckList.Count; i++)
                    {
                        List<CheckListItem> itemCheckList = checkList.Where(c => c.ID ==Convert.ToInt32( lstUserCheckList[i].CheckList)).ToList();
                        if (itemCheckList.Count > 0)
                        {
                            boardingList.Add(new OnBoarding
                            {
                                BoardingItemId = lstUserCheckList[i].Id,
                                BoardingItemName = itemCheckList[0].Name,
                                BoardingItemDesc = itemCheckList[0].Desc,
                                BoardingStatus = GetStatus(lstUserCheckList[i].CheckListStatus),
                                BoardingInternalName = itemCheckList[0].InternalName
                            });
                        }
                    }
                    if (showAssessments == "YES")
                    {
                        GetUserAssessments(boardingList);
                    }

                    if(showRoleBasedTraining=="YES")
                    {
                        List<UserTrainingDetail> roleTrainings = new List<UserTrainingDetail>();
                        GetUserRoleBasedTraining(ref roleTrainings, CurrentUser.DBUserId);
                        for (int i = 0; i < roleTrainings.Count; i++)
                        {
                            OnBoarding boardingItem = new OnBoarding();
                            boardingItem.BoardingItemId = roleTrainings[i].Id;
                            boardingItem.BoardingItemName = roleTrainings[i].TrainingName;
                            boardingItem.BoardingItemDesc = roleTrainings[i].ModuleDesc;
                            boardingItem.BoardingInternalName = roleTrainings[i].TrainingName;
                            boardingItem.BoardingIsMandatory = roleTrainings[i].Mandatory;
                            if (roleTrainings[i].IsLink)
                                boardingItem.BoardingItemLink = roleTrainings[i].LinkUrl;
                            else
                                boardingItem.BoardingItemLink = roleTrainings[i].DocumentUrl;
                            boardingItem.BoardingType = OnboardingItemType.RoleTraining;
                            boardingItem.BoardingStatus = Utilities.GetOnBoardingStatus(roleTrainings[i].IsTrainingCompleted, 0, Convert.ToDateTime(roleTrainings[i].CompletionDate));
                            boardingList.Add(boardingItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            return boardingList;
        }

        private OnboardingStatus GetStatus(object value)
        {
            if (Convert.ToString(value).ToLower().Trim() == "completed" || Convert.ToString(value).ToLower().Trim() == "true" || Convert.ToString(value).Trim() == "1")
            {
                return OnboardingStatus.Completed;
            }

            if (Convert.ToString(value).ToLower().Trim() == "initiated" || Convert.ToString(value).Trim() == "0")
            {
                return OnboardingStatus.OnGoing;
            }

            if (Convert.ToString(value).ToLower().Trim() == "rejected" || Convert.ToString(value).ToLower().Trim() == "false" || Convert.ToString(value).Trim() == "0")
            {
                return OnboardingStatus.Rejected;
            }

            return OnboardingStatus.NotStarted;
        }



        public int GetMarketRiskAssessmentID()
        {
            int assessmentId = 0;
            DataHelper dh = new DataHelper(strConnectionString);
            var assesmentId = 0;
            try
            {
                assesmentId = Convert.ToInt32(dh.ExecuteScalar("[dbo].[proc_GetMarketRiskAssesmentId]", CommandType.StoredProcedure, new SqlParameter("@UserID", CurrentUser.DBUserId), new SqlParameter("@AssessmentTitle", "Market Risk")));
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SqlSvrDAL, GetMarketRiskAssessmentID", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return assessmentId;
        }

        public List<object> UpdateOnBoardingStatus(List<OnBoardingTrainingStatus> objs)
        {
            List<object> lstOnboarding = new List<object>();
            UserManager manager = (UserManager)HttpContext.Current.Session["CurrentUser"];
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                if (objs != null && objs.Count > 0)
                {
                    foreach (OnBoardingTrainingStatus obj in objs)
                    {
                        if (obj.OnboardingType == OnboardingItemType.Training)
                        {
                            SqlParameter[] parameters = new SqlParameter[] {
                                new SqlParameter("@userID", manager.DBUserId),
                                new SqlParameter("@userTrainingID", obj.Id),
                                new SqlParameter("@CompletedDate", DateTime.Now)
                            };
                            DataHelper dhUpdate = new DataHelper(strConnectionString);
                            dhUpdate.ExecuteNonQuery("[dbo].[proc_UpdateOnboardingTrainingStatus]", CommandType.StoredProcedure, parameters);
                        }
                        else if (obj.OnboardingType == OnboardingItemType.RoleTraining)
                        {
                            SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@id", obj.Id) };
                            DataHelper dhUpdate = new DataHelper(strConnectionString);
                            dhUpdate.ExecuteNonQuery("[dbo].[proc_UpdateRoleTrainingStatus]", CommandType.StoredProcedure, parameters);
                        }
                        else if (obj.OnboardingType == OnboardingItemType.Default)
                        {
                            DataHelper dhCheck = new DataHelper(strConnectionString);
                            SqlParameter[] parameters = {new SqlParameter("@UserID",CurrentUser.DBUserId),
                                                         new SqlParameter("@CheckList",obj.Id)};
                            int ret = dhCheck.ExecuteNonQuery("[dbo].[proc_UpdateCheckListStatus]", CommandType.StoredProcedure, parameters);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return lstOnboarding;
        }


        public List<object> UpdateOnBoardingStatus(OnBoardingTrainingStatus obj)
        {
            UserManager manager = (UserManager)HttpContext.Current.Session["CurrentUser"];
            List<object> lstOnboarding = new List<object>();
            DataHelper dhUpdate = new DataHelper(strConnectionString);
            try
            {
                if (obj.OnboardingType == OnboardingItemType.Training)
                {
                    SqlParameter[] parameters = new SqlParameter[] {
                                new SqlParameter("@userID", manager.DBUserId),
                                new SqlParameter("@userTrainingID", obj.Id),
                                new SqlParameter("@CompletedDate", DateTime.Now)
                            };

                    int count = dhUpdate.ExecuteNonQuery("[dbo].[proc_UpdateOnboardingTrainingStatus]", CommandType.StoredProcedure, parameters);

                }
                else if (obj.OnboardingType == OnboardingItemType.Default)
                {

                    SqlParameter[] parameters = { new SqlParameter("@UserID", CurrentUser.DBUserId), new SqlParameter("@CheckList", obj.Id) };
                    int ret = dhUpdate.ExecuteNonQuery("[dbo].[proc_UpdateCheckListStatus]", CommandType.StoredProcedure, parameters);
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dhUpdate != null)
                {
                    if (dhUpdate.DataConn != null)
                    {
                        dhUpdate.DataConn.Close();
                    }
                }
            }
            return lstOnboarding;
        }

        public bool EmilOnBoardingStatus()
        {
            return false;
        }

        public void GetOnBoardingProfile()
        {
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                string onboardingid = string.Empty;
                SqlParameter[] parameters =
                {
                        new SqlParameter("@UserId", SqlDbType.Int) { Value =CurrentUser.DBUserId, Direction = ParameterDirection.Input },
                        new SqlParameter("@UserEmail", SqlDbType.NVarChar) { Value = CurrentUser.EmailID, Direction = ParameterDirection.Input }
                    };
                onboardingid = dh.ExecuteScalar("[dbo].[proc_GetOnBoardingProfile]", CommandType.StoredProcedure, parameters).ToString();

                if (onboardingid != String.Empty)
                {
                    CurrentUser.OnBoardingID = Convert.ToInt32(onboardingid);
                    CurrentUser.HasAttachments = false;

                }

            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetOnBoardingProfile", ex.Message, ex.StackTrace));

            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
        }

        public List<OnboardingHelp> GetOnboardingHelp()
        {
            DataHelper dh = new DataHelper(strConnectionString);
            List<OnboardingHelp> onboardHelp = new List<OnboardingHelp>();
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetOnBoardingHelp]", CommandType.StoredProcedure);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        OnboardingHelp item = new OnboardingHelp();
                        item.Description = row["Description"].ToString();
                        item.Title = row["Title"].ToString();
                        item.OrderingId = Convert.ToInt32(row["OrderingId"]);
                        onboardHelp.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return onboardHelp;
        }

        public List<Training> GetTrainings(int skillId, int competenceId)
        {
            List<Training> trainings = new List<Training>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                SqlParameter[] sqlParameters = new SqlParameter[2];
                sqlParameters[0] = new SqlParameter();
                sqlParameters[0].ParameterName = "@SkillID";
                sqlParameters[0].Value = skillId;
                sqlParameters[1] = new SqlParameter();
                sqlParameters[0].Direction = ParameterDirection.Input;
                sqlParameters[1].ParameterName = "@CompetenceID";
                sqlParameters[1].Value = competenceId;
                sqlParameters[1].Direction = ParameterDirection.Input;

                ds = dh.ExecuteDataSet("[dbo].[proc_GetTrainings]", CommandType.StoredProcedure, sqlParameters);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        Training item = new Training();
                        item.TrainingId = Convert.ToInt32(row["TrainingId"]);
                        item.TrainingName = row["TrainingName"].ToString();
                        item.SkillId = Convert.ToInt32(row["SkillID"]);
                        item.CompetencyId = Convert.ToInt32(row["CompetencyID"]);
                        if (!(row["Mandatory"] is DBNull))
                            item.IsMandatory = Convert.ToBoolean(row["Mandatory"]);
                        trainings.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return trainings;
        }

        public List<UserTraining> GetUserTrainingsByTrainingID(int TrainingId)
        {
            List<UserTraining> userTrainings = new List<UserTraining>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();

                ds = dh.ExecuteDataSet("[dbo].[proc_GetUserTrainingsByTrainingID]", CommandType.StoredProcedure, new SqlParameter("@TrainingId", TrainingId));
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        UserTraining userTraining = new UserTraining();
                        userTraining.Employee = row["Employee"].ToString();
                        if (row["IsTrainingCompleted"] != null)
                            userTraining.IsTrainingCompleted = Convert.ToBoolean(row["IsTrainingCompleted"]);
                        userTraining.SkillName = row["Skill"].ToString();
                        userTraining.TrainingName = row["Training"].ToString();
                        userTrainings.Add(userTraining);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return userTrainings;
        }

        public List<Assessment> GetAssessments(int skillId, int competenceId)
        {
            List<Assessment> assessments = new List<Assessment>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                SqlParameter[] sqlParameters = new SqlParameter[2];
                sqlParameters[0] = new SqlParameter();
                sqlParameters[0].ParameterName = "@SkillID";
                sqlParameters[0].Value = skillId;
                sqlParameters[0].Direction = ParameterDirection.Input;
                sqlParameters[1] = new SqlParameter();
                sqlParameters[1].ParameterName = "@CompetenceID";
                sqlParameters[1].Value = competenceId;
                sqlParameters[1].Direction = ParameterDirection.Input;

                ds = dh.ExecuteDataSet("[dbo].[proc_GetAssessments]", CommandType.StoredProcedure, sqlParameters);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        Assessment item = new Assessment();
                        if (row["AssessmentId"] != null && !(row["AssessmentId"] is DBNull))
                            item.AssessmentId = Convert.ToInt32(row["AssessmentId"]);
                        if (row["AssessmentName"] != null && !(row["AssessmentName"] is DBNull))
                            item.AssessmentName = row["AssessmentName"].ToString();
                        if (row["SkillID"] != null && !(row["SkillID"] is DBNull))
                            item.SkillId = Convert.ToInt32(row["SkillID"]);
                        if (row["CompetencyID"] != null && !(row["CompetencyID"] is DBNull))
                            item.CompetencyId = Convert.ToInt32(row["CompetencyID"]);
                        if (row["IsMandatory"] != null && !(row["IsMandatory"] is DBNull))
                            item.IsMandatory = Convert.ToBoolean(row["IsMandatory"]);
                        assessments.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return assessments;
        }

        public List<UserAssessment> GetUserAssessmentsByAssessmentId(int AssessmentId)
        {
            List<UserAssessment> userAssessments = new List<UserAssessment>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetUserAssessmentsByAssessmentID]", CommandType.StoredProcedure, new SqlParameter("@AssessmentId", AssessmentId));
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        UserAssessment userAssessment = new UserAssessment();
                        userAssessment.Employee = row["Employee"].ToString();
                        if (row["IsAssessmentCompleted"] != null)
                            userAssessment.IsAssessmentComplete = Convert.ToBoolean(row["IsAssessmentCompleted"]);
                        userAssessment.TrainingAssessment = row["Assessment"].ToString();
                        userAssessments.Add(userAssessment);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return userAssessments;
        }


        public void AddSkillBasedTrainingAssessment(string competence, int skillId, int userId)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@userId", userId), new SqlParameter("@skillId", skillId), new SqlParameter("@competencyLevelId", Convert.ToInt32(competence)) };
                DataHelper dh = new DataHelper(strConnectionString);
                int count = dh.ExecuteNonQuery("[dbo].[proc_AddSkillTrainingAssessment]", CommandType.StoredProcedure, parameters);

            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
        }

        public void AddSkillBasedTrainingAssessment(string competence, int skillId, int userId, bool isTrainingMandatory, DateTime lastDayOfCompletion)
        {

            try
            {
                SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@userId", userId), new SqlParameter("@skillId", skillId), new SqlParameter("@competenceId", Convert.ToInt32(competence)), new SqlParameter("@isMandatory", isTrainingMandatory), new SqlParameter("@lastdayofcompletion", lastDayOfCompletion) };
                DataHelper dh = new DataHelper(strConnectionString);
                int count = dh.ExecuteNonQuery("[dbo].[proc_AddSkillBasedTrainingAssessment]", CommandType.StoredProcedure, parameters);

            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }

        }

        public List<GEO> GetAllGEOs()
        {
            List<GEO> allGEOs = new List<GEO>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetAllGEOs]", CommandType.StoredProcedure);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                DataTable dt = dv.ToTable();
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        GEO item = new GEO();
                        item.Id = Int32.Parse(row["ID"].ToString());
                        item.Title = row["Title"].ToString();
                        //item.Skills = ??? 
                        allGEOs.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return allGEOs;
        }
        public void OnBoardUser(string competence, int skillId, int userId, string geo, int roleId, string userEmail, string userName)
        {
            object spResult = string.Empty;
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                int lastDayCompletion = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LastDayCompletion"]);
                SqlParameter[] parameters =
                {
                        new SqlParameter("@competenceid", SqlDbType.Int) { Value = Convert.ToInt32(competence), Direction = ParameterDirection.Input },
                        new SqlParameter("@skillid", SqlDbType.Int) { Value = skillId, Direction = ParameterDirection.Input  },
                        new SqlParameter("@geoid", SqlDbType.Int) { Value = Convert.ToInt32(geo), Direction = ParameterDirection.Input },
                        new SqlParameter("@useremail", SqlDbType.NVarChar) { Value = userEmail, Direction = ParameterDirection.Input },
                        new SqlParameter("@username", SqlDbType.NVarChar) { Value = userName, Direction = ParameterDirection.Input },
                        new SqlParameter("@roleid", SqlDbType.Int) { Value = roleId, Direction = ParameterDirection.Input },
                        new SqlParameter("@lastdayofcompletion", SqlDbType.Int) { Value = lastDayCompletion, Direction = ParameterDirection.Input }

                };
                spResult = dh.ExecuteScalar("[dbo].[proc_OnBoardUser]", CommandType.StoredProcedure, parameters);
            }
            catch (Exception ex)
            {
                UserManager userInfo = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, userInfo.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetEachQuestionDetails", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }

        }

        public UserOnBoarding GetOnBoardingDetailsForUser(UserManager user)
        {
            UserOnBoarding objUserOnBoarding = new UserOnBoarding();
            objUserOnBoarding.UserId = user.DBUserId;
            objUserOnBoarding.Name = user.UserName;
            objUserOnBoarding.Email = user.EmailID;
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                SqlParameter[] parameters =
                {
                        new SqlParameter("@UserId", SqlDbType.Int) { Value = user.DBUserId, Direction = ParameterDirection.Input },
                        new SqlParameter("@UserName", SqlDbType.NVarChar) { Value = user.UserName, Direction = ParameterDirection.Input  },
                        new SqlParameter("@UserEmail", SqlDbType.NVarChar) { Value = user.EmailID, Direction = ParameterDirection.Input }

                    };

                ds = dh.ExecuteDataSet("[dbo].[proc_GetOnBoardingDetailsForUser]", CommandType.StoredProcedure, parameters);
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow item in ds.Tables[0].Rows)
                        {
                            var TempLength = ds.Tables[0].Rows.Count;
                            objUserOnBoarding.CurrentSkill = Convert.ToString(item["CurrentSkill"]);
                            objUserOnBoarding.CurrentCompetance = Convert.ToString(item["CurrentCompetance"]);
                            objUserOnBoarding.CurrentStatus = Convert.ToString(item["CurrentStatus"]);
                            objUserOnBoarding.CurrentBGVStatus = Convert.ToString(item["CurrentBGVStatus"]);
                            objUserOnBoarding.CurrentProfileSharing = Convert.ToString(item["CurrentProfileSharing"]);
                            objUserOnBoarding.CurrentGEO = Convert.ToString(item["CurrentGEO"]);
                            objUserOnBoarding.IsPresentInOnBoard = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager userInfo = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, userInfo.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetEachQuestionDetails", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return objUserOnBoarding;
        }

        public List<UserAssessment> GetAssessmentForUser(int userId, bool OnlyOnBoardedTraining = false)
        {
            List<UserAssessment> userAssessments = new List<UserAssessment>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                OnlyOnBoardedTraining = true;
                SqlParameter[] sqlParameters = new SqlParameter[1];
                sqlParameters[0] = new SqlParameter();
                sqlParameters[0].ParameterName = "@UserId";
                sqlParameters[0].Value = userId;
                sqlParameters[0].Direction = ParameterDirection.Input;

                ds = dh.ExecuteDataSet("[dbo].[proc_GetAssessmentsForUser]", CommandType.StoredProcedure, sqlParameters);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        UserAssessment userAssessment = new UserAssessment();
                        if (row["Assessment"] != null && !(row["Assessment"] is DBNull))
                            userAssessment.TrainingAssessment = row["Assessment"].ToString();
                        if (row["CompletedDate"] != null && !(row["CompletedDate"] is DBNull))
                            userAssessment.CompletedDate = Convert.ToString(row["CompletedDate"]);
                        if (row["IsIncludeOnBoarding"] != null && !(row["IsIncludeOnBoarding"] is DBNull))
                            userAssessment.IsIncludeOnBoarding = Convert.ToBoolean(row["IsIncludeOnBoarding"]);
                        if (row["IsMandatory"] != null && !(row["IsMandatory"] is DBNull))
                            userAssessment.IsMandatory = Convert.ToBoolean(row["IsMandatory"]);
                        if (row["IsAssessmentActive"] != null && !(row["IsAssessmentActive"] is DBNull))
                            userAssessment.IsAssessmentActive = Convert.ToBoolean(row["IsAssessmentActive"]);
                        if (row["IsAssessmentComplete"] != null && !(row["IsAssessmentComplete"] is DBNull))
                            userAssessment.IsAssessmentComplete = Convert.ToBoolean(row["IsAssessmentComplete"]);
                        if (row["LastDayCompletion"] != null && !(row["LastDayCompletion"] is DBNull))
                            userAssessment.LastDayCompletion = Convert.ToDateTime(row["LastDayCompletion"].ToString()).ToShortDateString();
                        if (row["Skill"] != null && !(row["Skill"] is DBNull))
                            userAssessment.SkillName = row["Skill"] != null ? row["Skill"].ToString() : "";
                        if (row["SkillId"] != null && !(row["SkillId"] is DBNull))
                            userAssessment.SkillId = row["SkillId"] != null ? Convert.ToInt32(row["SkillId"]) : 0;
                        if (row["Training"] != null && !(row["Training"] is DBNull))
                            userAssessment.TrainingName = row["Training"] != null ? row["Training"].ToString() : "";
                        if (row["TrainingId"] != null && !(row["TrainingId"] is DBNull))
                            userAssessment.TrainingId= row["TrainingId"] != null ? Convert.ToInt32(row["TrainingId"]) : 0;
                        if (row["AssessmentId"] != null && !(row["AssessmentId"] is DBNull))
                            userAssessment.TrainingAssessmentId = row["AssessmentId"] != null ? Convert.ToInt32(row["AssessmentId"]) : 0;
                        userAssessments.Add(userAssessment);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return userAssessments;
        }

        public bool AssignAssessmentsToUser(List<UserAssessment> assessments, int userId, bool forDefault = false)
        {

            UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
            int ret = 0; bool result = false;
            string spResult = string.Empty;
            DataTable table = new DataTable();
            table.Columns.Add("TrainingAssessmentId", typeof(int));//
            table.Columns.Add("TrainingCourseId", typeof(int));//
            table.Columns.Add("IsMandatory", typeof(bool));//
            table.Columns.Add("IsIncludeOnBoarding", typeof(bool));//
            table.Columns.Add("LastDayCompletion", typeof(string));//

            // add a single row for each item in the collection.
            foreach (UserAssessment assessment in assessments)
            {
                table.Rows.Add(
                    assessment.TrainingAssessmentId,
                    assessment.SkillId,
                    assessment.IsMandatory,
                    assessment.IsIncludeOnBoarding,
                    assessment.LastDayCompletion

                    );
            }

            try
            {
                using (SqlConnection sqlcon = new SqlConnection(strConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.proc_AssignAssessmentsToUser", sqlcon))
                    {

                        sqlcon.Open();
                        // add the table-valued-parameter. 
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@ListAssessments", SqlDbType.Structured).Value = table;
                        cmd.Parameters.Add("@userid", SqlDbType.Int).Value = userId;
                        cmd.Parameters.Add("@fordefault", SqlDbType.Bit).Value = 1;
                        // execute sqlcon.Open();
                        ret = cmd.ExecuteNonQuery();
                        sqlcon.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager userInfo = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, userInfo.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetEachQuestionDetails", ex.Message, ex.StackTrace));
            }
            result = ret > 0 ? true : false;
            return result;
        }

        public List<UserManager> GetAllOnBoardedUser(int assignedTo)
        {
            List<UserManager> lstUserManager = new List<UserManager>();
            DataSet ds = new DataSet();
            DataView dv = new DataView();
            DataHelper dh = new DataHelper(strConnectionString);

            try
            {
                SqlParameter[] sqlParameters = new SqlParameter[1];
                sqlParameters[0] = new SqlParameter();
                sqlParameters[0].ParameterName = "@skillID";
                sqlParameters[0].Value = assignedTo;
                sqlParameters[0].Direction = ParameterDirection.Input;
                ds = dh.ExecuteDataSet("[dbo].[proc_GetAllOnBoardedUser]", CommandType.StoredProcedure, sqlParameters);

            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            UserManager objUser = null;
            if (ds.Tables.Count > 0)
            {
                if (ds.Tables[0].Rows.Count > 0)
                {
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        objUser = new UserManager()
                        {
                            EmailID = ds.Tables[0].Rows[i]["EmailAddress"].ToString(),
                            DBUserId = Convert.ToInt32(ds.Tables[0].Rows[i]["ID"].ToString()),
                            UserName = ds.Tables[0].Rows[i]["Name"].ToString()
                        };
                        lstUserManager.Add(objUser);
                    }
                }
            }
            return lstUserManager;
        }

        public bool AssignTrainingsToUser(List<UserTraining> trainings, int userId, bool forDefault = false)
        {
            UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
            int ret = 0; bool result = false;
            string spResult = string.Empty;
            DataTable table = new DataTable();
            table.Columns.Add("TrainingCourseId", typeof(int));//
            table.Columns.Add("TrainingModuleId", typeof(int));//
            table.Columns.Add("IsMandatory", typeof(bool));//
            table.Columns.Add("IsIncludeOnBoarding", typeof(bool));//
            table.Columns.Add("LastDayCompletion", typeof(string));//


            // add a single row for each item in the collection.
            foreach (UserTraining training in trainings)
            {
                table.Rows.Add(
                    training.SkillId,
                    training.TrainingId,
                    training.IsMandatory,
                    training.IsIncludeOnBoarding,
                    training.LastDayCompletion
                    );
            }



            try
            {
                using (SqlConnection sqlcon = new SqlConnection(strConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.proc_AssignTrainingsToUser", sqlcon))
                    {

                        sqlcon.Open();
                        // add the table-valued-parameter. 
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@ListTraining", SqlDbType.Structured).Value = table;
                        cmd.Parameters.Add("@userid", SqlDbType.Int).Value = userId;
                        cmd.Parameters.Add("@fordefault", SqlDbType.Bit).Value = 1;
                        // execute sqlcon.Open();
                        ret = cmd.ExecuteNonQuery();
                        sqlcon.Close();

                    }
                }
            }
            catch (Exception ex)
            {

                UserManager userInfo = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, userInfo.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetEachQuestionDetails", ex.Message, ex.StackTrace));

            }
            result = ret > 0 ? true : false;
            return result;

        }

        public void RemoveAssessmentHistory(int userId, int skillId, int competenceid)
        {
        }

        public bool AddSkill(string email, string userId, string skillId, string competence, bool ismandatory, DateTime lastdayofcompletion)
        {
            bool result = false;
            try
            {
                SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@userId", userId), new SqlParameter("@skillId", Convert.ToInt32(skillId)), new SqlParameter("@competenceId", Convert.ToInt32(competence)), new SqlParameter("@isMandatory", ismandatory), new SqlParameter("@lastdayofcompletion", lastdayofcompletion) };
                DataHelper dh = new DataHelper(strConnectionString);
                int count = dh.ExecuteNonQuery("[dbo].[proc_AddUserSkill]", CommandType.StoredProcedure, parameters);
                if (count > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            return result;
        }

        public List<UserSkill> GetSkillForUser(int userId)
        {
            return GetSkillForOnboardedUser(userId);
        }

        public List<UserSkill> GetSkillForOnboardedUser(int userId)
        {
            List<UserSkill> userSkills = new List<UserSkill>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetSkillForOnboardedUser]", CommandType.StoredProcedure, new SqlParameter("@userId", userId));

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        UserSkill objSkill = new UserSkill();
                        objSkill.Id = Int32.Parse(row["id"].ToString());
                        objSkill.Skill = row["skillName"].ToString();
                        objSkill.SkillId = Int32.Parse(row["skillId"].ToString());
                        objSkill.CompetenceId = Int32.Parse(row["competenceId"].ToString());
                        objSkill.Competence = row["competenceName"].ToString();
                        objSkill.SkillwiseCompetencies = row["skillwiseCompetencies"].ToString();
                        objSkill.SkillwiseCompetencyIds = row["skillwiseCompetencyIds"].ToString();
                        userSkills.Add(objSkill);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return userSkills;
        }

        public void UpdateUserSkill(int itemId, string competence, int userId, DateTime completiondate, bool isCompetenceChanged)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@userSkillId", Convert.ToInt32(itemId)), new SqlParameter("@userId", userId), new SqlParameter("@competencyLevelId", Convert.ToInt32(competence)), new SqlParameter("@completionDate", completiondate), new SqlParameter("@completencyChanged", isCompetenceChanged) };
                DataHelper dh = new DataHelper(strConnectionString);
                int count = dh.ExecuteNonQuery("[dbo].[proc_UpdateUserSkill]", CommandType.StoredProcedure, parameters);
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
        }

        public List<UserOnBoarding> GetOnBoardingDetailsReport(string status, bool isExcelDownload)
        {
            List<UserOnBoarding> lstUserOnBoarding = new List<UserOnBoarding>();
            DataSet ds = new DataSet();
            DataHelper dh = new DataHelper(strConnectionString);
            ds = dh.ExecuteDataSet("[dbo].[proc_GetAllUsers]", CommandType.StoredProcedure, new SqlParameter("@status", status));
            if (ds.Tables.Count > 0)
            {
                DataSet dsUserSkills = new DataSet();
                dsUserSkills = dh.ExecuteDataSet("proc_GetAllUserSkills", CommandType.StoredProcedure);

                List<UserSkill> lstSkills = new List<UserSkill>();
                UserSkill objSkill = null;
                if (dsUserSkills.Tables.Count > 0 && dsUserSkills.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow item in dsUserSkills.Tables[0].Rows)
                    {
                        objSkill = new UserSkill();
                        objSkill.Id = Convert.ToInt32(item["id"].ToString());
                        objSkill.Skill = item["Skill"].ToString();
                        objSkill.SkillId = Convert.ToInt32(item["SkillId"].ToString());
                        objSkill.Competence = item["Competence"].ToString();
                        objSkill.CompetenceId = Convert.ToInt32(item["CompetencyLevelId"].ToString());
                        objSkill.UserId = Convert.ToInt32(item["UserId"].ToString());
                        lstSkills.Add(objSkill);
                    }
                }
                DataSet dsUserTrainings = new DataSet();
                dsUserTrainings = dh.ExecuteDataSet("proc_GetAllUserTrainings", CommandType.StoredProcedure);
                List<UserTraining> lstTrainings = new List<UserTraining>();
                if (dsUserTrainings.Tables.Count > 0 && dsUserTrainings.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow item in dsUserTrainings.Tables[0].Rows)
                    {
                        UserTraining objTraining = new UserTraining();
                        objTraining.CompletedDate = Convert.ToString(item["CompletedDate"]);
                        if (item["IsIncludeOnBoarding"] != null && !(item["IsIncludeOnBoarding"] is DBNull))
                            objTraining.IsIncludeOnBoarding = Convert.ToBoolean(item["IsIncludeOnBoarding"]);
                        if (item["IsMandatory"] != null && !(item["IsMandatory"] is DBNull))
                            objTraining.IsMandatory = Convert.ToBoolean(item["IsMandatory"]);
                        if (item["IsTrainingActive"] != null && !(item["IsTrainingActive"] is DBNull))
                            objTraining.IsTrainingActive = Convert.ToBoolean(item["IsTrainingActive"]);
                        if (item["IsTrainingCompleted"] != null && !(item["IsTrainingCompleted"] is DBNull))
                            objTraining.IsTrainingCompleted = Convert.ToBoolean(item["IsTrainingCompleted"]);
                        if (item["LastDayCompletion"] != null && !(item["LastDayCompletion"] is DBNull))
                            objTraining.LastDayCompletion = Convert.ToString(item["LastDayCompletion"]);
                        if (item["Skill"] != null && !(item["Skill"] is DBNull))
                            objTraining.SkillName = item["Skill"].ToString();
                        if (item["SkillId"] != null && !(item["SkillId"] is DBNull))
                            objTraining.SkillId = Convert.ToInt32(item["SkillId"].ToString());
                        if (item["Training"] != null && !(item["Training"] is DBNull))
                            objTraining.TrainingName = item["Training"].ToString();
                        if (item["TrainingId"] != null && !(item["TrainingId"] is DBNull))
                            objTraining.TrainingId= Convert.ToInt32(item["TrainingId"].ToString());
                        if (item["UserId"] != null && !(item["UserId"] is DBNull))
                            objTraining.UserId = Convert.ToInt32(item["UserId"].ToString());

                        if (objTraining.IsTrainingCompleted)
                        {
                            objTraining.StatusColor = AppConstant.Green;
                            objTraining.ItemStatus = AppConstant.Completed;
                        }
                        else if (objTraining.IsTrainingCompleted == false && Convert.ToDateTime(objTraining.LastDayCompletion) < DateTime.Now)
                        {
                            objTraining.StatusColor = AppConstant.Red;
                            objTraining.ItemStatus = AppConstant.OverDue;
                        }
                        else
                        {
                            objTraining.StatusColor = AppConstant.Amber;
                            objTraining.ItemStatus = AppConstant.InProgress;
                        }
                        lstTrainings.Add(objTraining);
                    }
                }
                DataSet dsUserAssessments = new DataSet();
                dsUserAssessments = dh.ExecuteDataSet("proc_GetAllUserAssessments", CommandType.StoredProcedure);

                List<UserAssessment> lstAssessments = new List<UserAssessment>();
                if (dsUserAssessments.Tables.Count > 0 && dsUserAssessments.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow item in dsUserAssessments.Tables[0].Rows)
                    {
                        UserAssessment objUserAssessment = new UserAssessment();
                        objUserAssessment.CompletedDate = Convert.ToString(item["CompletedDate"]);
                        if (item["IsIncludeOnBoarding"] != null && !(item["IsIncludeOnBoarding"] is DBNull))
                            objUserAssessment.IsIncludeOnBoarding = Convert.ToBoolean(item["IsIncludeOnBoarding"]);
                        if (item["IsMandatory"] != null && !(item["IsMandatory"] is DBNull))
                            objUserAssessment.IsMandatory = Convert.ToBoolean(item["IsMandatory"]);
                        if (item["IsAssessmentActive"] != null && !(item["IsAssessmentActive"] is DBNull))
                            objUserAssessment.IsAssessmentActive = Convert.ToBoolean(item["IsAssessmentActive"]);
                        if (item["IsAssessmentComplete"] != null && !(item["IsAssessmentComplete"] is DBNull))
                            objUserAssessment.IsAssessmentComplete = Convert.ToBoolean(item["IsAssessmentComplete"]);
                        if (item["LastDayCompletion"] != null && !(item["LastDayCompletion"] is DBNull))
                            objUserAssessment.LastDayCompletion = Convert.ToString(item["LastDayCompletion"]);
                        if (item["Skill"] != null && !(item["Skill"] is DBNull))
                            objUserAssessment.SkillName = item["Skill"].ToString();
                        if (item["SkillId"] != null && !(item["SkillId"] is DBNull))
                            objUserAssessment.SkillId = Convert.ToInt32(item["SkillId"].ToString());
                        if (item["Training"] != null && !(item["Training"] is DBNull))
                            objUserAssessment.TrainingName = item["Training"].ToString();
                        if (item["TrainingId"] != null && !(item["TrainingId"] is DBNull))
                            objUserAssessment.TrainingId = Convert.ToInt32(item["TrainingId"]);
                        if (item["Assessment"] != null && !(item["Assessment"] is DBNull))
                            objUserAssessment.TrainingAssessment = item["Assessment"].ToString();
                        if (item["AssessmentId"] != null && !(item["AssessmentId"] is DBNull))
                            objUserAssessment.TrainingAssessmentId = Convert.ToInt32(item["AssessmentId"].ToString());
                        if (item["UserId"] != null && !(item["UserId"] is DBNull))
                            objUserAssessment.UserId = Convert.ToInt32(item["UserId"].ToString());

                        if (objUserAssessment.IsAssessmentComplete)
                        {
                            objUserAssessment.StatusColor = AppConstant.Green;
                            objUserAssessment.ItemStatus = AppConstant.Completed;
                        }
                        else if (objUserAssessment.IsAssessmentComplete == false && Convert.ToDateTime(objUserAssessment.LastDayCompletion) < DateTime.Now)
                        {
                            objUserAssessment.StatusColor = AppConstant.Red;
                            objUserAssessment.ItemStatus = AppConstant.OverDue;
                        }
                        else
                        {
                            objUserAssessment.StatusColor = AppConstant.Amber;
                            objUserAssessment.ItemStatus = AppConstant.InProgress;
                        }
                        lstAssessments.Add(objUserAssessment);
                    }
                }

                foreach (DataRow item in ds.Tables[0].Rows)
                {
                    UserOnBoarding objUserOnBoarding = new UserOnBoarding();
                    if (item["Skill"] != null && !(item["Skill"] is DBNull))
                        objUserOnBoarding.CurrentSkill = item["Skill"].ToString();
                    if (item["GEO"] != null && !(item["GEO"] is DBNull))
                        objUserOnBoarding.CurrentGEO = item["GEO"].ToString();
                    if (item["CompetencyLevel"] != null && !(item["CompetencyLevel"] is DBNull))
                        objUserOnBoarding.CurrentCompetance = item["CompetencyLevel"].ToString();
                    if (item["Status"] != null && !(item["Status"] is DBNull))
                        objUserOnBoarding.CurrentStatus = item["Status"].ToString();
                    if (item["BGVStatus"] != null && !(item["BGVStatus"] is DBNull))
                        objUserOnBoarding.CurrentBGVStatus = item["BGVStatus"].ToString();
                    if (item["ProfileSharing"] != null && !(item["ProfileSharing"] is DBNull))
                        objUserOnBoarding.CurrentProfileSharing = item["ProfileSharing"].ToString();
                    if (item["EmailAddress"] != null && !(item["EmailAddress"] is DBNull))
                        objUserOnBoarding.Email = item["EmailAddress"].ToString();
                    if (item["ID"] != null && !(item["ID"] is DBNull))
                        objUserOnBoarding.UserId = Convert.ToInt32(item["ID"].ToString());
                    if (item["Name"] != null && !(item["Name"] is DBNull))
                        objUserOnBoarding.Name = item["Name"].ToString();
                    if (item["ProjectId"] != null && !(item["ProjectId"] is DBNull))
                        objUserOnBoarding.ProjectId = Convert.ToInt32(item["ProjectId"].ToString());
                    if (item["Project"] != null && !(item["Project"] is DBNull))
                        objUserOnBoarding.ProjectName = item["Project"].ToString();

                    if (isExcelDownload)
                    {
                        List<UserSkill> lstUserSkills = lstSkills.Where(s => s.UserId == objUserOnBoarding.UserId).ToList();
                        objUserOnBoarding.UserSkills = lstUserSkills;

                        List<UserTraining> lstUserTrainings = lstTrainings.Where(s => s.UserId == objUserOnBoarding.UserId).ToList();
                        objUserOnBoarding.UserTrainings = lstUserTrainings;

                        List<UserAssessment> lstUserAssessments = lstAssessments.Where(s => s.UserId == objUserOnBoarding.UserId).ToList();
                        objUserOnBoarding.UserAssessments = lstUserAssessments;
                    }
                    lstUserOnBoarding.Add(objUserOnBoarding);
                }
            }
            return lstUserOnBoarding;
        }

        public List<Competence> GetCompetenciesBySkillId(int SkillID)
        {
            List<Competence> competencies = new List<Competence>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();

                ds = dh.ExecuteDataSet("[dbo].[proc_GetCompetenciesBySkillId]", CommandType.StoredProcedure, new SqlParameter("@SkillID", SkillID));
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        Competence item = new Competence();
                        if (row["ID"] != null && !(row["ID"] is DBNull))
                            item.CompetenceId = Int32.Parse(row["ID"].ToString());
                        if (row["Title"] != null && !(row["Title"] is DBNull))
                            item.CompetenceName = row["Title"].ToString();

                        competencies.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return competencies;
        }


        public List<Competence> GetCompetenciesBySkillName(string name)
        {
            List<Competence> competencies = new List<Competence>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();

                ds = dh.ExecuteDataSet("[dbo].[proc_GetCompetenciesBySkillName]", CommandType.StoredProcedure, new SqlParameter("@SkillName", name));
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        Competence item = new Competence();
                        item.CompetenceId = Int32.Parse(row["ID"].ToString());
                        item.CompetenceName = row["Title"].ToString();
                        competencies.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return competencies;
        }

        private List<UserAssessment> GetUserAssessments(int userId)
        {
            List<UserAssessment> userAssessmentCollection = new List<UserAssessment>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                SqlParameter[] parameters =
                {
                        new SqlParameter("@UserID", SqlDbType.Int) { Value = userId, Direction = ParameterDirection.Input }
                  };

                ds = dh.ExecuteDataSet("[dbo].[proc_GetAssessmentsForUser]", CommandType.StoredProcedure, parameters);
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow item in ds.Tables[0].Rows)
                        {
                            UserAssessment userAssessment = new UserAssessment();
                            if(item["AssessmentId"]!=null && !(item["AssessmentId"] is DBNull))
                                userAssessment.Id = Convert.ToInt32(item["AssessmentId"].ToString());
                            if (item["Assessment"] != null && !(item["Assessment"] is DBNull))
                                userAssessment.TrainingAssessment = item["Assessment"].ToString();
                            if (item["SkillId"] != null && !(item["SkillId"] is DBNull))
                                userAssessment.SkillId = Convert.ToInt32(item["SkillId"].ToString());
                            if (item["IsAssessmentComplete"] != null && !(item["IsAssessmentComplete"] is DBNull))
                                userAssessment.IsAssessmentComplete = Convert.ToBoolean(item["IsAssessmentComplete"]);
                            
                            userAssessmentCollection.Add(userAssessment);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserAssessments", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return userAssessmentCollection;
        }
        public void CacheConfig()
        {

        }
        public List<UserAssessment> GetUserAssessmentsByID(int ID)
        {
            List<UserAssessment> userAssessments = new List<UserAssessment>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetUserAssessmentsByID]", CommandType.StoredProcedure, new SqlParameter("@ID", ID));
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        UserAssessment item = new UserAssessment();
                        item.TrainingAssessment = row["Assessment"].ToString();
                        if (!(row["CompletedDate"] is DBNull))
                            item.CompletedDate = Convert.ToDateTime(row["CompletedDate"]).ToString();
                        if (!(row["MarksInPercentage"] is DBNull))
                            item.MarksInPercentage = Convert.ToDecimal(row["MarksInPercentage"]);
                        userAssessments.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return userAssessments;
        }

        public string GetUserCompetencyLabel(int userid)
        {
            object competencyName = String.Empty;
            DataHelper dh = new DataHelper(strConnectionString);
            UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
            try
            {
                competencyName = dh.ExecuteScalar("[dbo].[proc_GetUserCompetancyLabel]", CommandType.StoredProcedure, new SqlParameter("@userid", userid));
            }
            catch (Exception ex)
            {
                users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return competencyName.ToString();
        }

        public List<AcademyJoinersCompletion> GetCurrentUserAssessments(string listName, int? Id, bool updateAttempt)
        {
            List<AcademyJoinersCompletion> academyJoiners = new List<AcademyJoinersCompletion>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetCurrentUserAssessments]", CommandType.StoredProcedure, new SqlParameter("@UserId", CurrentUser.DBUserId));

                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        if (Id == null)
                        {
                            foreach (DataRow row in ds.Tables[0].Rows)
                            {

                                AcademyJoinersCompletion item = new AcademyJoinersCompletion();
                                if (row["SkillID"] != null)
                                {
                                    if (!String.IsNullOrEmpty(row["SkillID"].ToString()))
                                        item.TrainingCourseLookUpId = Int32.Parse(row["SkillID"].ToString());
                                }
                                if (row["Skill"] != null)
                                {
                                    if (!String.IsNullOrEmpty(row["Skill"].ToString()))
                                        item.TrainingCourseLookUpText = row["Skill"].ToString();
                                }
                                if (row["LastDayCompletion"] != null)
                                {
                                    if (!String.IsNullOrEmpty(row["LastDayCompletion"].ToString()) && !(row["LastDayCompletion"] is DBNull))
                                        item.LastDayCompletion = Convert.ToDateTime(row["LastDayCompletion"]);
                                }
                                if (row["IsMandatory"] != null)
                                {
                                    if (!String.IsNullOrEmpty(row["IsMandatory"].ToString()))
                                        item.IsMandatory = Convert.ToBoolean(row["IsMandatory"]);
                                }
                                if (row["IsAssessmentComplete"] != null)
                                {
                                    if (!String.IsNullOrEmpty(row["IsAssessmentComplete"].ToString()))
                                        item.AssessmentStatus = Convert.ToBoolean(row["IsAssessmentComplete"]);
                                }
                                if (row["NoOfAttempt"] != null)
                                {
                                    if (!String.IsNullOrEmpty(row["NoOfAttempt"].ToString()))
                                        item.Attempts = Convert.ToInt32(row["NoOfAttempt"].ToString());
                                }
                                if (row["MarksObtained"] != null)
                                {
                                    if (!String.IsNullOrEmpty(row["MarksObtained"].ToString()))
                                        item.MarksSecured = Convert.ToInt32(row["MarksObtained"]);
                                }
                                item.MaxAttempts = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxAttempts"]) == -1 ? int.MaxValue : Convert.ToInt32(ConfigurationManager.AppSettings["MaxAttempts"]);
                                if (row["AssessmentID"] != null)
                                {
                                    if (!String.IsNullOrEmpty(row["AssessmentID"].ToString()))
                                    {
                                        item.TrainingAssessmentLookUpId = Convert.ToInt32(row["AssessmentID"]);
                                        item.Id = Convert.ToInt32(row["AssessmentID"]);
                                    }
                                }
                                if (row["Assessment"] != null)
                                {
                                    if (!String.IsNullOrEmpty(row["Assessment"].ToString()))
                                        item.TrainingAssessmentLookUpText = row["Assessment"].ToString();
                                }
                                academyJoiners.Add(item);
                            }
                        }
                        else
                        {
                            foreach (DataRow row in ds.Tables[0].Rows)
                            {
                                if (Id == Convert.ToInt32(row["AssessmentID"].ToString()))
                                {
                                    AcademyJoinersCompletion item = new AcademyJoinersCompletion();
                                    if (row["SkillID"] != null)
                                    {
                                        if (!String.IsNullOrEmpty(row["SkillID"].ToString()))
                                            item.TrainingCourseLookUpId = Int32.Parse(row["SkillID"].ToString());
                                    }
                                    if (row["Skill"] != null)
                                    {
                                        if (!String.IsNullOrEmpty(row["Skill"].ToString()))
                                            item.TrainingCourseLookUpText = row["Skill"].ToString();
                                    }
                                    if (row["LastDayCompletion"] != null)
                                    {
                                        if (!String.IsNullOrEmpty(row["LastDayCompletion"].ToString()) && !(row["LastDayCompletion"] is DBNull))
                                            item.LastDayCompletion = Convert.ToDateTime(row["LastDayCompletion"]);
                                    }
                                    if (row["IsMandatory"] != null)
                                    {
                                        if (!String.IsNullOrEmpty(row["IsMandatory"].ToString()))
                                            item.IsMandatory = Convert.ToBoolean(row["IsMandatory"]);
                                    }
                                    if (row["IsAssessmentComplete"] != null)
                                    {
                                        if (!String.IsNullOrEmpty(row["IsAssessmentComplete"].ToString()))
                                            item.AssessmentStatus = Convert.ToBoolean(row["IsAssessmentComplete"]);
                                    }
                                    if (row["NoOfAttempt"] != null)
                                    {
                                        if (!String.IsNullOrEmpty(row["NoOfAttempt"].ToString()))
                                            item.Attempts = Convert.ToInt32(row["NoOfAttempt"].ToString());
                                    }
                                    if (row["MarksObtained"] != null)
                                    {
                                        if (!String.IsNullOrEmpty(row["MarksObtained"].ToString()))
                                            item.MarksSecured = Convert.ToInt32(row["MarksObtained"]);
                                    }
                                    item.MaxAttempts = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxAttempts"]) == -1 ? int.MaxValue : Convert.ToInt32(ConfigurationManager.AppSettings["MaxAttempts"]);
                                    if (row["AssessmentID"] != null)
                                    {
                                        if (!String.IsNullOrEmpty(row["AssessmentID"].ToString()))
                                        {
                                            item.TrainingAssessmentLookUpId = Convert.ToInt32(row["AssessmentID"]);
                                            item.Id = Convert.ToInt32(row["AssessmentID"]);
                                        }
                                    }
                                    if (row["Assessment"] != null)
                                    {
                                        if (!String.IsNullOrEmpty(row["Assessment"].ToString()))
                                            item.TrainingAssessmentLookUpText = row["Assessment"].ToString();
                                    }
                                    academyJoiners.Add(item);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, CurrentUser.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return academyJoiners;
        }
        public List<UserTrainingDetail> GetTrainingItems()
        {
            List<UserTrainingDetail> userTrainingDetails = new List<UserTrainingDetail>();
            userTrainingDetails = GetTrainingDetails();
            return userTrainingDetails;

        }
        private List<UserTrainingDetail> GetTrainingDetails()
        {
            List<UserTrainingDetail> trainingDetails = new List<UserTrainingDetail>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetTrainingDetails]", CommandType.StoredProcedure, new SqlParameter("@userID", CurrentUser.DBUserId));
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        UserTrainingDetail item = new UserTrainingDetail();
                        item.SkillId = Int32.Parse(row["SkillId"].ToString());
                        item.SkillName = row["Skill"].ToString();
                        item.TrainingId = Int32.Parse(row["TrainingID"].ToString());
                        item.TrainingName = row["TrainingName"].ToString();
                        if (!((row["CompletedDate"]) is DBNull))
                            item.CompletionDate = Convert.ToDateTime(row["CompletedDate"]).ToShortDateString();

                        item.IsLink = true;
                        item.DocumentUrl = null;
                        if (row["IsMandatory"] != null)
                            item.Mandatory = Convert.ToBoolean(row["IsMandatory"]);
                        if (row["IsTrainingCompleted"] != null)
                        {
                            if (!String.IsNullOrEmpty(row["IsTrainingCompleted"].ToString()))
                                item.IsTrainingCompleted = Convert.ToBoolean(row["IsTrainingCompleted"]);
                        }

                        if (item.IsTrainingCompleted)
                        {
                            item.status = Utilities.GetTraningStatus(item.IsTrainingCompleted, item.NoOfAttempts, item.LastDayToComplete);
                        }
                        if (row["LastDayCompletion"] != null)
                        {
                            if (!String.IsNullOrEmpty(row["LastDayCompletion"].ToString()))
                            {
                                //  item.LastDayToComplete = DateTime.ParseExact(row["LastDayCompletion"].ToString(), "dd/MM/yy", CultureInfo.InvariantCulture);                                
                                item.LastDayToComplete = Convert.ToDateTime(row["LastDayCompletion"]);
                            }
                        }
                        item.bgColor = Utilities.GetTrainingColor(item.status);
                        item.TrainingType = TrainingType.SkillTraining;
                        if (row["TrainingLink"] != null)
                            item.LinkUrl = row["TrainingLink"].ToString();
                        trainingDetails.Add(item);
                    }
                }


            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            GetUserRoleBasedTraining(ref trainingDetails, CurrentUser.DBUserId);
            return trainingDetails;
        }
        public void GetUserRoleBasedTraining(ref List<UserTrainingDetail> trainings, int userid)
        {
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetUserRoleBasedTraining]", CommandType.StoredProcedure, new SqlParameter("@userID", CurrentUser.DBUserId));
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                DataTable dt = dv.ToTable();
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        UserTrainingDetail item = new UserTrainingDetail();
                        item.SkillId = Int32.Parse(row["RoleId"].ToString());
                        item.SkillName = row["Role"].ToString();
                        item.TrainingId = Int32.Parse(row["TrainingID"].ToString());
                        item.TrainingName = row["TrainingName"].ToString();
                        if (!((row["CompletedDate"]) is DBNull))
                            item.CompletionDate = Convert.ToDateTime(row["CompletedDate"]).ToShortDateString();

                        item.IsLink = true;
                        item.DocumentUrl = null;
                        if (row["IsMandatory"] != null)
                            item.Mandatory = Convert.ToBoolean(row["IsMandatory"]);
                        if (row["IsTrainingCompleted"] != null)
                        {
                            if (!String.IsNullOrEmpty(row["IsTrainingCompleted"].ToString()))
                                item.IsTrainingCompleted = Convert.ToBoolean(row["IsTrainingCompleted"]);
                        }

                        if (item.IsTrainingCompleted)
                        {
                            item.status = Utilities.GetTraningStatus(item.IsTrainingCompleted, item.NoOfAttempts, item.LastDayToComplete);
                        }
                        if (row["LastDayCompletion"] != null)
                        {
                            if (!String.IsNullOrEmpty(row["LastDayCompletion"].ToString()))
                            {
                                item.LastDayToComplete = Convert.ToDateTime(row["LastDayCompletion"]);
                            }
                        }
                        item.bgColor = Utilities.GetTrainingColor(item.status);
                        item.TrainingType = TrainingType.RoleTraining;
                        if (row["URL"] != null)
                            item.LinkUrl = row["URL"].ToString();
                        trainings.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
        }

        public List<UserSkillDetail> GetUserTrainingsDetails(string SPlistName)
        {
            List<UserSkillDetail> trainingCourses = new List<UserSkillDetail>();
            UserManager user = (UserManager)System.Web.HttpContext.Current.Session["CurrentUser"];
            try
            {
                List<UserTrainingDetail> userTrainings = GetTrainingDetails();
                if (userTrainings != null && userTrainings.Count > 0)
                {
                    trainingCourses = GetCourseWiseTrainingModules(userTrainings);

                    return trainingCourses.OrderBy(x => x.LastDayToComplete).ToList();
                }
            }
            catch (Exception ex)
            {
                UserManager loggeduser = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, loggeduser.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserTrainingsDetails", ex.Message, ex.StackTrace));

            }
            return trainingCourses;
        }

        public List<UserSkillDetail> GetTrainingJourneyDetails(string SPlistName, int userId)
        {
            List<UserSkillDetail> userSkills = new List<UserSkillDetail>();
            UserManager userManager = (UserManager)HttpContext.Current.Session["CurrentUser"];
            //List<UserSkillDetail> trainingCourses = new List<UserSkillDetail>();

            List<UserTrainingDetail> userTrainings = GetSkillBasedTrainingsList(userId);
            List<UserAssessment> userAssessments = GetUserAssessments(userId);

            if (userTrainings != null && userTrainings.Count > 0)
            {
                userSkills = GetSkillWiseTrainings(userTrainings);

                foreach (var skill in userSkills)
                {
                    if (skill.listOfTraining != null)
                    {
                        skill.LastDayToComplete = (DateTime)skill.listOfTraining
                                                                .OrderByDescending(x => x.LastDayToComplete).Take(1)
                                                                .Select(x => x.LastDayToComplete)
                                                                .Single();
                        List<UserAssessment> skillwiseAssessments = userAssessments.Where(u => u.SkillId == skill.id).ToList();
                        skill.skillStatus = Utilities.GetCourseStatus(skill.listOfTraining, skillwiseAssessments); // FailedOngoing, OverdueOngoing- RED|Blue

                    }
                }
                return userSkills.OrderBy(x => x.LastDayToComplete).ToList();
            }
            return userSkills;
        }

        public List<SkillCompetencies> GetSkillCompetencyTraingings()
        {
            List<SkillCompetencies> trainings = new List<SkillCompetencies>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetSkillCompetencyTrainings]", CommandType.StoredProcedure);

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    DataTable dt = ds.Tables[0];
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            SkillCompetencies skillCompetencies = new SkillCompetencies();
                            skillCompetencies.CompetenceId = Convert.ToInt32(row["CompetencyID"]);
                            skillCompetencies.CompetenceName = row["CompetencyLevel"].ToString();
                            skillCompetencies.TrainingDescription = row["Description"].ToString();
                            skillCompetencies.TrainingId = Convert.ToInt32(row["TrainingId"]);
                            skillCompetencies.TrainingName = row["TrainingName"].ToString();
                            skillCompetencies.SkillId = Convert.ToInt32(row["SkillId"].ToString());
                            trainings.Add(skillCompetencies);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return trainings;
        }

        public List<UserRole> GetRoleForOnboardedUser(int userId)
        {
            List<UserRole> lstUserRoles = new List<UserRole>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetUserRoles]", CommandType.StoredProcedure, new SqlParameter("@UserId", userId));
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    DataTable dt = ds.Tables[0];
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            UserRole u = new UserRole();
                            u.RoleId = Convert.ToInt32(row["ID"].ToString());
                            u.RoleName = row["TITLE"].ToString();
                            lstUserRoles.Add(u);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return lstUserRoles;
        }

        public bool AddRole(string email, string userId, string roleId, bool ismandatory, DateTime lastdayofcompletion)
        {
            bool result = false;
            try
            {
                SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@userId", userId), new SqlParameter("@roleId", roleId), new SqlParameter("@isMandatory", ismandatory), new SqlParameter("@lastdayofcompletion", lastdayofcompletion) };
                DataHelper dh = new DataHelper(strConnectionString);
                int count = dh.ExecuteNonQuery("[dbo].[proc_AddUserRole]", CommandType.StoredProcedure, parameters);
                if (count > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            return result;
        }

        public void RemoveUserRole(int roleId, string userId)
        {

            try
            {
                SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@userId", userId), new SqlParameter("@roleId", roleId) };
                DataHelper dh = new DataHelper(strConnectionString);
                dh.ExecuteNonQuery("[dbo].[proc_RemoveUserRole]", CommandType.StoredProcedure, parameters);
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
        }

        public List<Project> GetAllProjects()
        {
            List<Project> projects = new List<Project>();
            DataSet ds = new DataSet();
            DataView dv = new DataView();
            DataHelper dh = new DataHelper(strConnectionString);

            try
            {
                ds = dh.ExecuteDataSet("[dbo].[proc_GetAllProjects]", CommandType.StoredProcedure);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        Project item = new Project();
                        item.ID = Int32.Parse(row["ID"].ToString());
                        item.ProjectName = row["Title"].ToString();
                        //item.Skills = ??? 
                        projects.Add(item);
                    }
                }
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return projects;
        }

        public void AddProject(string projectName)
        {
            DataHelper dh = new DataHelper(strConnectionString);
            SqlParameter[] parameters =
            {
                new SqlParameter("@Title",SqlDbType.VarChar ) { Value = projectName}
            };
            try
            {
                dh.ExecuteNonQuery("[dbo].[proc_AddProject]", CommandType.StoredProcedure, parameters);
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
        }

        public void UpdateProject(Project project)
        {
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {

                SqlParameter[] parameters =
                    {
                    new SqlParameter("@ProjectID",SqlDbType.Int),
                    new SqlParameter("@Title",SqlDbType.VarChar ),
                    new SqlParameter("@ErrorNumber",SqlDbType.Int),
                    new SqlParameter("@ErrorMessage",SqlDbType.VarChar),
                };
                parameters[0].Value = project.ID;
                parameters[1].Value = project.ProjectName;
                parameters[2].Direction = ParameterDirection.Output;
                parameters[3].Size = 4000;
                parameters[3].Direction = ParameterDirection.Output;
                dh.ExecuteNonQuery("[dbo].[proc_UpdateProject]", CommandType.StoredProcedure, parameters);

                if (dh.Cmd != null && dh.Cmd.Parameters["@ErrorNumber"].Value != DBNull.Value && dh.Cmd.Parameters["@ErrorMessage"].Value != DBNull.Value)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SqlSvrDAL, UpdateProject", dh.Cmd.Parameters["@ErrorNumber"].Value.ToString(), dh.Cmd.Parameters["@ErrorMessage"].Value.ToString()));
                }
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
        }

        public void RemoveProject(int projectID)
        {
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {

                SqlParameter[] parameters =
                    {
                    new SqlParameter("@ProjectID",SqlDbType.Int),
                    new SqlParameter("@ErrorNumber",SqlDbType.Int),
                    new SqlParameter("@ErrorMessage",SqlDbType.VarChar),
                };
                parameters[0].Value = projectID;
                parameters[1].Direction = ParameterDirection.Output;
                parameters[2].Size = 4000;
                parameters[2].Direction = ParameterDirection.Output;
                dh.ExecuteNonQuery("[dbo].[proc_DeleteProject]", CommandType.StoredProcedure, parameters);

                if (dh.Cmd != null && dh.Cmd.Parameters["@ErrorNumber"].Value != DBNull.Value && dh.Cmd.Parameters["@ErrorMessage"].Value != DBNull.Value)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SqlSvrDAL, RemoveProject", dh.Cmd.Parameters["@ErrorNumber"].Value.ToString(), dh.Cmd.Parameters["@ErrorMessage"].Value.ToString()));
                }

            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
        }

        public Project EditProjectByID(int projectID)
        {
            Project project = new Project();
            project.ID = projectID;
            DataHelper dh = new DataHelper(strConnectionString);
            DataSet ds = new DataSet();
            DataView dv = new DataView();
            try
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@ProjectID",SqlDbType.Int)
                };
                parameters[0].Value = project.ID;
                ds = dh.ExecuteDataSet("[dbo].[proc_GetProjectById]", CommandType.StoredProcedure, parameters);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();
                if (dt != null && dt.Rows.Count > 0)
                {
                    project.ProjectName = dt.Rows[0]["Title"].ToString();
                }
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return project;
        }

        public Resource GetResourceDetailsByProjectID(int projectID)
        {
            Resource resource = new Resource();
            resource.projectId = projectID;
            resource.allResources = new List<AllResources>();
            DataSet ds = new DataSet();
            DataView dv = new DataView();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                SqlParameter[] parameters =
               {
                    new SqlParameter("@ProjectID",SqlDbType.Int),
                    new SqlParameter("@ProjectName",SqlDbType.VarChar)
                };
                parameters[0].Value = projectID;
                parameters[1].Size = 255;
                parameters[1].Direction = ParameterDirection.Output;

                ds = dh.ExecuteDataSet("[dbo].[proc_GetAvailableSkillResourceCountByProjectId]", CommandType.StoredProcedure, parameters);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                resource.projectName = Convert.ToString(dh.Cmd.Parameters["@ProjectName"].Value);
                Hashtable objHashTable = new Hashtable();
                DataTable dt = dv.ToTable();
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (!objHashTable.ContainsKey(dr["SkillId"]))
                        {
                            objHashTable.Add(dr["SkillId"], dr["SkillId"]);
                            AllResources objAllResources = new AllResources();
                            objAllResources.skillId = Convert.ToInt32(dr["SkillId"]);
                            objAllResources.skill = dr["SkillName"].ToString();
                            resource.allResources.Add(objAllResources);
                        }
                    }
                }
                foreach (AllResources item in resource.allResources)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (item.skillId == Convert.ToInt32(dr["SkillId"]))
                        {
                            switch (dr["CompetencyLevel"].ToString().ToUpper())
                            {
                                case "NOVICE":
                                    item.expectedBeginnerCount = Convert.ToInt32(dr["ExpectedResourceCount"].ToString() == String.Empty ? "0" : dr["ExpectedResourceCount"].ToString());
                                    item.availableBeginnerCount = Convert.ToInt32(dr["AvailableResourceCount"].ToString() == String.Empty ? "0" : dr["AvailableResourceCount"].ToString());
                                    break;
                                case "ADVANCED BEGINNER":
                                    item.ExpectedadvancedBeginnerCount = Convert.ToInt32(dr["ExpectedResourceCount"].ToString() == String.Empty ? "0" : dr["ExpectedResourceCount"].ToString());
                                    item.AvailableadvancedBeginnerCount = Convert.ToInt32(dr["AvailableResourceCount"].ToString() == String.Empty ? "0" : dr["AvailableResourceCount"].ToString());
                                    break;

                                case "COMPETENT":
                                    item.ExpectedcompetentCount = Convert.ToInt32(dr["ExpectedResourceCount"].ToString() == String.Empty ? "0" : dr["ExpectedResourceCount"].ToString());
                                    item.AvailablecompetentCount = Convert.ToInt32(dr["AvailableResourceCount"].ToString() == String.Empty ? "0" : dr["AvailableResourceCount"].ToString());
                                    break;

                                case "PROFICIENT":
                                    item.expectedProficientCount = Convert.ToInt32(dr["ExpectedResourceCount"].ToString() == String.Empty ? "0" : dr["ExpectedResourceCount"].ToString());
                                    item.availableProficientCount = Convert.ToInt32(dr["AvailableResourceCount"].ToString() == String.Empty ? "0" : dr["AvailableResourceCount"].ToString());
                                    break;

                                case "EXPERT":
                                    item.expectedExpertCount = Convert.ToInt32(dr["ExpectedResourceCount"].ToString() == String.Empty ? "0" : dr["ExpectedResourceCount"].ToString());
                                    item.availableExpertCount = Convert.ToInt32(dr["AvailableResourceCount"].ToString() == String.Empty ? "0" : dr["AvailableResourceCount"].ToString());
                                    break;
                                default:
                                    break;
                            }
                        }

                    }
                }
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return resource;
        }

        public ProjectDetails GetProjectSkillsByProjectID(string projectID)
        {
            ProjectDetails objProjectDetails = new ProjectDetails();
            DataSet ds = new DataSet();
            DataView dv = new DataView();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                SqlParameter[] parameters =
               {
                    new SqlParameter("@ProjectID",SqlDbType.Int),
                    new SqlParameter("@ProjectName",SqlDbType.VarChar)
                };
                parameters[0].Value = Int32.Parse(projectID);
                parameters[1].Size = 255;
                parameters[1].Direction = ParameterDirection.Output;

                ds = dh.ExecuteDataSet("[dbo].[proc_GetSkillsByProjectId]", CommandType.StoredProcedure, parameters);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                objProjectDetails.ProjectId = Int32.Parse(projectID);
                objProjectDetails.ProjectName = Convert.ToString(dh.Cmd.Parameters["@ProjectName"].Value);
                objProjectDetails.ProjectSkill = new List<ProjectSkill>();
                ProjectSkill objProjectSkill = null;

                DataTable dt = dv.ToTable();
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        objProjectSkill = new ProjectSkill();

                        objProjectSkill.ItemId = Int32.Parse(row["ItemId"].ToString());
                        objProjectSkill.Project = Convert.ToString(dh.Cmd.Parameters["@ProjectName"].Value);
                        objProjectSkill.ProjectId = Int32.Parse(row["ProjectId"].ToString());
                        objProjectSkill.Skill = row["SkillName"].ToString();
                        objProjectSkill.SkillId = Int32.Parse(row["SkillId"].ToString());
                        objProjectDetails.ProjectSkill.Add(objProjectSkill);
                    }
                }
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return objProjectDetails;
        }

        public ProjectSkill PostProjectSkill(string projectid, string skillid)
        {
            ProjectSkill objProjectSkill = new ProjectSkill();
            DataHelper dh = new DataHelper(strConnectionString);
            SqlParameter[] parameters =
            {
                new SqlParameter("@ProjectId",SqlDbType.Int ) { Value = Int32.Parse(projectid) },
                new SqlParameter("@SkillId",SqlDbType.Int){Value = Int32.Parse(skillid)}
            };
            try
            {
                dh.ExecuteNonQuery("[dbo].[proc_AddProjectSkill]", CommandType.StoredProcedure, parameters);
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return objProjectSkill;
        }

        public void DeleteProjectSkill(int projectskillid, string projectid, string skillid)
        {
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {

                SqlParameter[] parameters =
                    {
                    new SqlParameter("@ProjectSkillID",SqlDbType.Int),
                    new SqlParameter("@ErrorNumber",SqlDbType.Int),
                    new SqlParameter("@ErrorMessage",SqlDbType.VarChar),
                };
                parameters[0].Value = projectskillid;
                parameters[1].Direction = ParameterDirection.Output;
                parameters[2].Size = 4000;
                parameters[2].Direction = ParameterDirection.Output;
                dh.ExecuteNonQuery("[dbo].[proc_DeleteProjectSkill]", CommandType.StoredProcedure, parameters);

                if (dh.Cmd != null && dh.Cmd.Parameters["@ErrorNumber"].Value != DBNull.Value && dh.Cmd.Parameters["@ErrorMessage"].Value != DBNull.Value)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SqlSvrDAL, DeleteProjectSkill", dh.Cmd.Parameters["@ErrorNumber"].Value.ToString(), dh.Cmd.Parameters["@ErrorMessage"].Value.ToString()));
                }

            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
        }

        public void AddProjectSkillResources(ProjectResources prjRes)
        {
            DataHelper dh = new DataHelper(strConnectionString);
            foreach (var item in prjRes.skillResources)
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@ProjectId", SqlDbType.Int ) { Value = prjRes.projectId },
                    new SqlParameter("@SkillId", SqlDbType.Int) { Value = item.skillId},
                    new SqlParameter("@AvlNoviceCount", SqlDbType.Int) { Value = item.beginnerCount },
                    new SqlParameter("@AvlAdvancedBeginnerCount", SqlDbType.Int) { Value = item.advancedBeginnerCount },
                    new SqlParameter("@AvlCompetentCount", SqlDbType.Int) { Value = item.competentCount },
                    new SqlParameter("@AvlProficientCount", SqlDbType.Int) { Value = item.proficientCount },
                    new SqlParameter("@AvlExpertCount", SqlDbType.Int) { Value = item.expertCount}
                };
                try
                {
                    dh.ExecuteNonQuery("[dbo].[proc_AddAvlResourceCountByProjectID]", CommandType.StoredProcedure, parameters);
                }
                finally
                {
                    if (dh != null)
                    {
                        if (dh.DataConn != null)
                        {
                            dh.DataConn.Close();
                        }
                    }
                }
            }
        }

        public void AddExpectedProjectResourceCountByProjectId(ProjectResources prjRes)
        {
            DataHelper dh = new DataHelper(strConnectionString);
            foreach (var item in prjRes.skillResources)
            {
                SqlParameter[] parameters =
                {
                    new SqlParameter("@ProjectId", SqlDbType.Int ) { Value = prjRes.projectId },
                    new SqlParameter("@SkillId", SqlDbType.Int) { Value = item.skillId},
                    new SqlParameter("@ExptdNoviceCount", SqlDbType.Int) { Value = item.beginnerCount },
                    new SqlParameter("@ExptdAdvancedBeginnerCount", SqlDbType.Int) { Value = item.advancedBeginnerCount },
                    new SqlParameter("@ExptdCompetentCount", SqlDbType.Int) { Value = item.competentCount },
                    new SqlParameter("@ExptdProficientCount", SqlDbType.Int) { Value = item.proficientCount },
                    new SqlParameter("@ExptdExpertCount", SqlDbType.Int) { Value = item.expertCount}
                };
                try
                {
                    dh.ExecuteNonQuery("[dbo].[proc_AddExpecedResourceCountByProjectID]", CommandType.StoredProcedure, parameters);
                }
                finally
                {
                    if (dh != null)
                    {
                        if (dh.DataConn != null)
                        {
                            dh.DataConn.Close();
                        }
                    }
                }
            }
        }

        public ProjectResources GetExpectedProjectResourceCountByProjectId(ProjectResources prjRes)
        {
            DataSet ds = new DataSet();
            DataView dv = new DataView();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                SqlParameter[] parameters =
               {
                    new SqlParameter("@ProjectID",SqlDbType.Int),
                    new SqlParameter("@ProjectName",SqlDbType.VarChar)
                };
                parameters[0].Value = prjRes.projectId;
                parameters[1].Size = 255;
                parameters[1].Direction = ParameterDirection.Output;

                ds = dh.ExecuteDataSet("[dbo].[proc_GetExpectedSkillResourceCountByProjectId]", CommandType.StoredProcedure, parameters);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }

            prjRes.projectName = Convert.ToString(dh.Cmd.Parameters["@ProjectName"].Value);
            List<SkillResource> lstSkillResource = new List<SkillResource>();
            Hashtable objHashTable = new Hashtable();
            DataTable dt = dv.ToTable();
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    if (!objHashTable.ContainsKey(dr["SkillId"]))
                    {
                        objHashTable.Add(dr["SkillId"], dr["SkillId"]);
                        SkillResource objSkillResource = new SkillResource();
                        objSkillResource.skillId = Convert.ToInt32(dr["SkillId"]);
                        objSkillResource.skill = dr["SkillName"].ToString();
                        lstSkillResource.Add(objSkillResource);
                    }
                }
            }
            prjRes.skillResources = lstSkillResource;

            foreach (SkillResource skr in prjRes.skillResources)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    if (skr.skillId == Convert.ToInt32(dr["SkillId"]))
                    {
                        switch (dr["CompetencyLevel"].ToString().ToUpper())
                        {
                            case "NOVICE":
                                skr.beginnerCount = Convert.ToInt32(dr["ExpectedResourceCount"].ToString() == String.Empty ? "0" : dr["ExpectedResourceCount"].ToString());
                                break;
                            case "ADVANCED BEGINNER":
                                skr.advancedBeginnerCount = Convert.ToInt32(dr["ExpectedResourceCount"].ToString() == String.Empty ? "0" : dr["ExpectedResourceCount"].ToString());
                                break;

                            case "COMPETENT":
                                skr.competentCount = Convert.ToInt32(dr["ExpectedResourceCount"].ToString() == String.Empty ? "0" : dr["ExpectedResourceCount"].ToString());
                                break;

                            case "PROFICIENT":
                                skr.proficientCount = Convert.ToInt32(dr["ExpectedResourceCount"].ToString() == String.Empty ? "0" : dr["ExpectedResourceCount"].ToString());
                                break;

                            case "EXPERT":
                                skr.expertCount = Convert.ToInt32(dr["ExpectedResourceCount"].ToString() == String.Empty ? "0" : dr["ExpectedResourceCount"].ToString());
                                break;
                            default:
                                break;
                        }
                    }

                }
            }
            return prjRes;
        }

        public bool OnboardEmail(string email, int UserId, string UserName)
        {
            bool result = false;
            try
            {
                Hashtable hashtable = new Hashtable();
                Hashtable Training_HT = new Hashtable();

                string strAssessmentname = string.Empty;

                List<UserAssessment> AssessmentLi = GetAssessmentForUser(UserId);
                foreach (UserAssessment itemAsses in AssessmentLi)
                {
                    DateTime dt = Convert.ToDateTime(itemAsses.LastDayCompletion);
                    strAssessmentname = strAssessmentname + itemAsses.TrainingAssessment + " Last Completion Date " + dt.ToString("MMMM dd") + ", ";
                }
                string strTrainingName = string.Empty;
                List<UserTraining> trainingLi = GetTrainingForUser(UserId, true);
                foreach (UserTraining itemTraining in trainingLi)
                {
                    DateTime dt = Convert.ToDateTime(itemTraining.LastDayCompletion);
                    strTrainingName = strTrainingName + itemTraining.TrainingName + " Last Completion Date " + dt.ToString("MMMM dd") + ", ";
                }

                hashtable.Add("UserName", UserName);
                hashtable.Add("ClientName", ConfigurationManager.AppSettings["ClientName"].ToString());
                hashtable.Add("WebUrl", "");
                hashtable.Add("Assessment", strAssessmentname);
                hashtable.Add("Training", strTrainingName);
                bool Queue1 = AddToEmailQueue("EmployeeOnboardMail", hashtable, email, null);



             //   List<UserTraining> traningLi = GetTrainingForUser(UserId, true);
                string trainingTable = string.Empty;
                trainingTable += "<table border='1' cellspacing='0' cellpadding='0' style='border-collapse: collapse; border: none;'>";
                trainingTable += "<tbody>";
                trainingTable += "<tr>";
                trainingTable += "<td><b>Training Name</b></td>";
                trainingTable += "<td><b>Last Date of Completion</b></td>";
                trainingTable += "<td><b>Mandatory?</b></td>";
                trainingTable += "</tr>";

                foreach (UserTraining item in trainingLi)
                {
                    trainingTable += "<tr>";
                    trainingTable += "<td>" + item.TrainingName + "</td>";
                    //     trainingTable += "<td>" + item.TrainingCourse + "</td>";
                    trainingTable += "<td>" + item.LastDayCompletion + "</td>";
                    trainingTable += "<td>" + item.IsMandatory + "</td>";
                    trainingTable += "</tr>";
                }
                trainingTable += "</tbody>";
                trainingTable += "</table>";
                Hashtable data = new Hashtable();
                data.Add("TrainingTable", trainingTable);
                data.Add("UserName", UserName);
                data.Add("ClientName", ConfigurationManager.AppSettings["ClientName"].ToString());
                data.Add("WebUrl", "");
                data.Add("Assessment", strAssessmentname);
                bool Queue2 = AddToEmailQueue("EmployeeOnboardTrainingMail", data, email, null);
                result = true;
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, OnboardEmail", ex.Message, ex.StackTrace));
            }
            return result;
        }
        #region PrivateMethods

        private List<SkillCompetencies> GetSkillCompetenciesByName(string Skill)
        {
            List<SkillCompetencies> skillCompetencies = new List<SkillCompetencies>();
            List<Competence> competences = GetCompetenciesBySkillName(Skill);
            var ulHTML = new TagBuilder("ul");
            StringBuilder output = new StringBuilder();
            return skillCompetencies;
        }

        private List<UserSkillDetail> GetCourseWiseTrainingModules(List<UserTrainingDetail> trainingModuleList)
        {
            UserManager user = CurrentUser;
            List<UserSkillDetail> skills = new List<UserSkillDetail>();
            var courseList = trainingModuleList.Select(x => x.SkillName).Distinct();

            foreach (var courseItem in courseList)
            {
                UserSkillDetail skill = new UserSkillDetail();
                skill.skillName = courseItem;

                skill.listOfTraining = (List<UserTrainingDetail>)trainingModuleList.Where(x => x.SkillName == courseItem)
                    .Select(x => new UserTrainingDetail
                    {
                        SkillName = x.SkillName,
                        TrainingName = x.TrainingName,
                        NoOfAttempts = x.NoOfAttempts,
                        LastDayToComplete = x.LastDayToComplete,
                        IsTrainingCompleted = x.IsTrainingCompleted,
                        status = x.status,
                        bgColor = x.bgColor,
                        IsLink = x.IsLink,
                        IsWikiLink = x.IsWikiLink,
                        LinkUrl = x.LinkUrl,
                        DocumentUrl = x.DocumentUrl,
                        TrainingType = x.TrainingType
                    }).ToList();
                skill.id = skill.listOfTraining[0].SkillId;
                skill.TrainingType = skill.listOfTraining[0].TrainingType;
                skills.Add(skill);
            }
            return skills;
        }

        private List<UserTrainingDetail> GetSkillBasedTrainingsList(int userId)
        {
            List<UserTrainingDetail> trainingsLst = new List<UserTrainingDetail>();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                ds = dh.ExecuteDataSet("[dbo].[proc_GetSkillBasedTraining]", CommandType.StoredProcedure, new SqlParameter("@UserId", CurrentUser.DBUserId));
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                ArrayList defaultTrainingIds = new ArrayList();
                ArrayList defaultAssessmentIds = new ArrayList();                
                DataTable dt = dv.ToTable();

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {   
                        UserTrainingDetail item = new UserTrainingDetail();
                        item.IsLink = false;
                        item.DocumentUrl = null;
                        if(row["TrainingName"] != null && !(row["TrainingName"] is DBNull))
                            item.TrainingName= row["TrainingName"].ToString();
                        if (row["SkillID"] != null && !(row["SkillID"] is DBNull))
                            item.SkillId = Int32.Parse(row["SkillID"].ToString());
                        if (row["Skill"] != null && !(row["Skill"] is DBNull))
                            item.SkillName = row["Skill"].ToString();
                        if(row["CompletedDate"] != null && !(row["CompletedDate"] is DBNull))                        
                          item.CompletionDate = Convert.ToDateTime(row["CompletedDate"].ToString()).ToShortDateString();
                        
                        if(row["IsMandatory"] != null && !(row["IsMandatory"] is DBNull))
                          item.Mandatory = Convert.ToBoolean(row["IsMandatory"]);
                        
                        if(row["IsTrainingCompleted"] != null && !(row["IsTrainingCompleted"] is DBNull))                        
                            item.IsTrainingCompleted = Convert.ToBoolean(row["IsTrainingCompleted"]);

                        if(item.IsTrainingCompleted)
                        {
                            item.status = Utilities.GetTraningStatus(item.IsTrainingCompleted, item.NoOfAttempts, item.LastDayToComplete);
                        }
                        if(row["LastDayCompletion"] != null && !(row["LastDayCompletion"] is DBNull))
                            item.LastDayToComplete = Convert.ToDateTime(row["LastDayCompletion"]);
                        
                        item.bgColor = Utilities.GetTrainingColor(item.status);
                        item.TrainingType = TrainingType.SkillTraining;
                        trainingsLst.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return trainingsLst;
        }
        private List<UserDetails> GetUserDetails(int skillId, int competenceId)
        {
            List<Training> trainings = new List<Training>();
            List<Training> lstAllTrainings = new List<Training>();
            List<UserDetails> userDetails = new List<UserDetails>();
            List<UserDetails> userTrainings = new List<UserDetails>();
            List<UserDetails> allDetails = new List<UserDetails>();
            DataSet ds = new DataSet();
            DataView dv = new DataView();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                SqlParameter[] parameters =
               {
                    new SqlParameter("@SkillId",SqlDbType.Int),
                    new SqlParameter("@CompetencyId",SqlDbType.Int)
                };
                parameters[0].Value = skillId;
                parameters[1].Value = competenceId;

                ds = dh.ExecuteDataSet("[dbo].[proc_GetSkillCompetencyLevelTraining]", CommandType.StoredProcedure, parameters);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                DataTable dt = new DataTable();
                dt = dv.ToTable();
                if (dt != null & dt.Rows.Count > 0)
                {
                    UserDetails allskill = null;
                    foreach (DataRow dr in dt.Rows)
                    {
                        allskill = new UserDetails();
                        allskill.TrainingId = Convert.ToInt32(dr["TrainingId"]);
                        allskill.TrainingName = Convert.ToString(dr["TrainingName"]);
                        if (!(dr["IsMandatory"] is DBNull))
                            allskill.IsMandatory = Convert.ToBoolean(dr["IsMandatory"]);
                        allskill.skillName = Convert.ToString(dr["SkillName"]);
                        allskill.competenceName = Convert.ToString(dr["CompetenceName"]);
                        userDetails.Add(allskill);
                        Training training = new Training();
                        training.TrainingId = allskill.TrainingId;
                        training.TrainingName = allskill.TrainingName;
                        training.IsMandatory = allskill.IsMandatory;
                        lstAllTrainings.Add(training);
                    }
                    if (lstAllTrainings.Count > 0)
                    {
                        List<UserDetails> lstAllUsers = new List<UserDetails>();
                        List<UserDetails> lstTrainedUsers = new List<UserDetails>();
                        lstAllUsers = GetUsersForAllTrainings();             //Fetching user details corresponding to the Trainings
                        foreach (Training train in lstAllTrainings)
                        {
                            var trainUsers = lstAllUsers.Where(user => user.TrainingCourse == train.TrainingName).ToList();
                            lstTrainedUsers.AddRange(trainUsers);
                        }
                        userTrainings.AddRange(lstTrainedUsers);
                    }
                }
                if (userTrainings.Count > 0)                                            //Merging the Training and corresponding Users data.
                {
                    var lstDetails = (from s1 in userDetails
                                      join s2 in userTrainings
                 on s1.TrainingName equals s2.TrainingCourse
                                      select new UserDetails()
                                      {
                                          TrainingId = s1.TrainingId,
                                          TrainingName = s1.TrainingName,
                                          IsMandatory = s1.IsMandatory,
                                          Employee = s2.Employee,
                                          IsTrainingCompleted = s2.IsTrainingCompleted,
                                          TrainingCourse = s2.TrainingCourse,
                                          skillName = s1.skillName,
                                          competenceName = s1.competenceName
                                      }).ToList();
                    allDetails = lstDetails;
                }
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return allDetails;
        }

        private List<UserDetails> GetUsersForAllTrainings()
        {
            List<UserDetails> lstUsersDetails = new List<UserDetails>();
            //Tables:UserTraining,SkillCompetencyLevelTraining,AcademyOnboarding
            DataSet ds = new DataSet();
            DataView dv = new DataView();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                ds = dh.ExecuteDataSet("[dbo].[proc_GetUsersForAllTrainings]", CommandType.StoredProcedure);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);

                DataTable dt = new DataTable();
                dt = dv.ToTable();
                UserDetails objTraining = null;
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        objTraining = new UserDetails();
                        objTraining.TrainingId = Convert.ToInt32(dr["TrainingId"]);
                        objTraining.IsTrainingCompleted = Convert.ToBoolean(dr["IsTrainingCompleted"]);
                        objTraining.TrainingCourse = Convert.ToString(dr["Training"]);
                        objTraining.Employee = Convert.ToString(dr["EmployeeName"]);
                        objTraining.skillName = Convert.ToString(dr["SkillName"]);
                        lstUsersDetails.Add(objTraining);
                    }
                }
            }
            finally
            {
                if (dh != null)
                {
                    if (dh.DataConn != null)
                    {
                        dh.DataConn.Close();
                    }
                }
            }
            return lstUsersDetails;
        }

        private List<TrainingCount> GetTrainingCounts(List<UserDetails> userTrainings)
        {
            List<TrainingCount> lstCount = new List<TrainingCount>();
            List<TrainingCount> trainingCounts = new List<TrainingCount>();
            var allCount = userTrainings.GroupBy(train => new { train.TrainingName, train.IsTrainingCompleted, train.competenceName }).Select(grp =>
            new
            {
                trainingDetail = grp.Key,
                total = grp.Count()
            }).ToList();            //Categorizing the Users based on Training Name and Completion status

            if (allCount.Count() > 0)           //If users exist for particular training
            {
                foreach (var train in allCount)
                {
                    int comp = 0;
                    int prog = 0;
                    if (train.trainingDetail.IsTrainingCompleted)
                        comp = train.total;
                    else
                        prog = train.total;
                    lstCount.Add(new TrainingCount()
                    {
                        trainingName = train.trainingDetail.TrainingName,
                        competencyLevel = train.trainingDetail.competenceName,
                        completedCount = comp,
                        progressCount = prog
                    });
                }

                int i = 0;
                if (lstCount.Count > 1)     //For trainings which have Users who have Completed as well as Work in Progress status
                {
                    while ((i + 1) < lstCount.Count)                        //Normalizing count based on Training name to avoid multiple rows
                    {
                        if (lstCount[i].trainingName == lstCount[i + 1].trainingName)
                        {
                            lstCount[i].trainingName = lstCount[i].trainingName;
                            lstCount[i].competencyLevel = lstCount[i].competencyLevel;
                            lstCount[i].completedCount = lstCount[i].completedCount + lstCount[i + 1].completedCount;
                            lstCount[i].progressCount = lstCount[i].progressCount + lstCount[i + 1].progressCount;
                            lstCount.RemoveAt(i + 1);
                        }
                        i++;
                    }
                    trainingCounts = lstCount;
                }
                else                               //For trainings which have users with either Completed/Work in Progress status
                    return lstCount;
            }
            return trainingCounts;
        }

        private List<UserSkillDetail> GetSkillWiseTrainings(List<UserTrainingDetail> trainingModuleList)
        {
            UserManager user = CurrentUser;
            List<UserSkillDetail> skills = new List<UserSkillDetail>();
            var courseList = trainingModuleList.Select(x => x.SkillName).Distinct();

            foreach (var courseItem in courseList)
            {
                UserSkillDetail skill = new UserSkillDetail();
                skill.skillName = courseItem;

                skill.listOfTraining = (List<UserTrainingDetail>)trainingModuleList.Where(x => x.SkillName == courseItem)
                    .Select(x => new UserTrainingDetail
                    {
                        SkillName = x.SkillName,
                        TrainingName = x.TrainingName,
                        NoOfAttempts = x.NoOfAttempts,
                        LastDayToComplete = x.LastDayToComplete,
                        IsTrainingCompleted = x.IsTrainingCompleted,
                        status = x.status,
                        bgColor = x.bgColor,
                        IsLink = x.IsLink,
                        IsWikiLink = x.IsWikiLink,
                        LinkUrl = x.LinkUrl,
                        DocumentUrl = x.DocumentUrl,
                        TrainingType = x.TrainingType
                    }).ToList();
                skill.id = skill.listOfTraining[0].SkillId;
                skill.TrainingType = skill.listOfTraining[0].TrainingType;
                skills.Add(skill);
            }
            return skills;
        }

        private List<CheckListItem> GetOnBoardingCheckList(string locValue, int locId)
        {
            List<CheckListItem> checklistItems = GetAllChecklist();
            List<CheckListItem> filteredItems = checklistItems.Where(c => c.GEOId == locId).ToList();
            return filteredItems;
        }

        public List<CheckListItem> GetAllChecklist()
        {

            List<CheckListItem> items = HttpContext.Current.Session[AppConstant.AllCheckListData] as List<CheckListItem>;
            if (items == null)
            {
                items = new List<CheckListItem>();

                DataSet dsCheckList = new DataSet();
                DataView dvCheckList = new DataView();
                DataHelper dhCheckList = new DataHelper(strConnectionString);

                dsCheckList = dhCheckList.ExecuteDataSet("[dbo].[proc_GetAllChecklists]", CommandType.StoredProcedure);
                dvCheckList = new DataView(dsCheckList.Tables[0]);
                DataTable dtCheckList = dvCheckList.ToTable();
                if (dtCheckList != null && dtCheckList.Rows.Count != 0)
                {
                    foreach (DataRow row in dtCheckList.Rows)
                    {
                        CheckListItem item = new CheckListItem();
                        item.ID = Convert.ToInt32(row["ID"].ToString());
                        if (Convert.ToBoolean(row["TypeChoice"]))
                            item.Choice = "TRUE";
                        else
                            item.Choice = "FALSE";
                        item.Desc = row["Description"].ToString();
                        item.GEOId = Convert.ToInt32(row["GEOID"]);
                        item.GEOName = row["GEO"].ToString();
                        item.InternalName = row["InternalName"].ToString();
                        item.RoleId = Convert.ToInt32(row["RoleID"]);
                        item.RoleName = row["Role"].ToString();
                        item.Name = row["Title"].ToString();
                        items.Add(item);
                    }
                }
                HttpContext.Current.Session[AppConstant.AllCheckListData] = items;
            }
            return items;
        }
        private List<OnBoarding> GetUserAssessments(List<OnBoarding> boardingList)
        {
            List<int> ids = new List<int>();
            List<UserAssessment> userAssessments = new List<UserAssessment>();
            List<Assessment> allAssessments = new List<Assessment>();
            allAssessments = GetAllAssessments();
            DataHelper dh = new DataHelper(strConnectionString);
            try
            {
                DataSet ds = new DataSet();
                DataView dv = new DataView();
                SqlParameter[] sqlParameters = new SqlParameter[2];
                sqlParameters[0] = new SqlParameter();
                sqlParameters[0].ParameterName = "@UserId";
                sqlParameters[0].Value = CurrentUser.DBUserId;
                sqlParameters[0].Direction = ParameterDirection.Input;
                sqlParameters[1] = new SqlParameter();
                sqlParameters[1].ParameterName = "@OnlyOnBoardedTraining";
                sqlParameters[1].Value = true;

                ds = dh.ExecuteDataSet("[dbo].[proc_GetAssessmentsForUser]", CommandType.StoredProcedure, sqlParameters);
                if (ds.Tables.Count > 0)
                    dv = new DataView(ds.Tables[0]);
                DataTable dt = dv.ToTable();
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        UserAssessment userAssessment = new UserAssessment();

                        Assessment assessmentItem = new Assessment();

                        bool assessmentStatus = false;
                        if (row["Assessment"] != null && !(row["Assessment"] is DBNull))
                            userAssessment.TrainingAssessment = row["Assessment"].ToString();
                        if (row["CompletedDate"] != null && !(row["CompletedDate"] is DBNull))
                            userAssessment.CompletedDate = Convert.ToString(row["CompletedDate"]);
                        if (row["IsAssessmentComplete"] != null && !(row["IsAssessmentComplete"] is DBNull))
                        {
                            assessmentStatus = true;
                            userAssessment.IsAssessmentComplete = Convert.ToBoolean(row["IsAssessmentComplete"]);
                        }
                        if (row["AssessmentId"] != null && !(row["AssessmentId"] is DBNull))
                            userAssessment.TrainingAssessmentId = row["AssessmentId"] != null ? Convert.ToInt32(row["AssessmentId"]) : 0;
                        userAssessments.Add(userAssessment);
                        int attempts = 0;
                        DateTime date = new DateTime();
                        if (row["NoOfAttempt"] != null && !(row["NoOfAttempt"] is DBNull))
                            attempts = Convert.ToInt32(row["NoOfAttempt"]);
                        if (row["LastDayCompletion"] != null && !(row["LastDayCompletion"] is DBNull))
                            date = Convert.ToDateTime(row["LastDayCompletion"]);
                        if (assessmentItem != null)
                        {
                            OnBoarding board = new OnBoarding();
                            board.BoardingItemId = Convert.ToInt32(row["ID"]);
                            board.BoardingItemName = assessmentItem.AssessmentName;
                            board.BoardingItemDesc = assessmentItem.Description;
                            board.BoardingStatus = (OnboardingStatus)Utilities.GetOnBoardingStatus
                                                    (
                                                        assessmentStatus,
                                                        attempts,
                                                        date
                                                    );

                            if (board.BoardingStatus == OnboardingStatus.Completed || board.BoardingStatus == OnboardingStatus.Failed ||
                                board.BoardingStatus == OnboardingStatus.OverDue || board.BoardingStatus == OnboardingStatus.Rejected)
                            {
                                board.BoardingItemLink = "#";
                            }
                            else
                            {
                                board.BoardingItemLink = assessmentItem.AssessmentLink;
                            }
                            board.BoardingType = OnboardingItemType.Assessment;
                            board.BoardIngAssessmentId = assessmentItem.AssessmentId;
                            boardingList.Add(board);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));
            }
            return boardingList;
        }
        private List<Assessment> GetAllAssessments()
        {
            List<Assessment> assessments = new List<Assessment>();

            try
            {
                DataSet dsAssessments = new DataSet();
                DataView dvAssessments = new DataView();
                DataHelper dh = new DataHelper(strConnectionString);
                dsAssessments = dh.ExecuteDataSet("[dbo].[proc_GetAllAssessments]", CommandType.StoredProcedure);
                if (dsAssessments.Tables.Count > 0)
                    dvAssessments = new DataView(dsAssessments.Tables[0]);
                DataTable dtAssessment = dvAssessments.ToTable();

                if (dtAssessment != null & dtAssessment.Rows.Count > 0)
                {
                    Assessment assessment = null;
                    foreach (DataRow item in dtAssessment.Rows)
                    {
                        string _AssessmentLink = null;
                        if (item["AssessmentLink"] != null)
                        {
                            _AssessmentLink = item["AssessmentLink"].ToString();
                        }

                        int _AssessmentTimeInMins = 0;
                        if (item["AssessmentTimeInMins"] != null)
                        {
                            _AssessmentTimeInMins = Convert.ToInt32(item["AssessmentTimeInMins"].ToString());
                        }

                        assessment = new Assessment();

                        assessment.AssessmentId = Convert.ToInt32(item["ID"].ToString());
                        assessment.AssessmentName = Convert.ToString(item["Title"]);
                        if (item["IsMandatory"] != null)
                            assessment.IsMandatory = Convert.ToBoolean(item["IsMandatory"]);
                        assessment.AssessmentLink = _AssessmentLink;
                        assessment.AssessmentTimeInMins = _AssessmentTimeInMins;
                        if (item["Description"] != null)
                            assessment.Description = Convert.ToString(item["Description"]);
                        assessments.Add(assessment);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllAssessments", ex.Message, ex.StackTrace));

            }
            return assessments;
        }


        #endregion
    }
}