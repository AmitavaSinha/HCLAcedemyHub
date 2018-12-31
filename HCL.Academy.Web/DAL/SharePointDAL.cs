using HCL.Academy.Web.Models;
using HCLAcademy.Common;
using HCLAcademy.Models;
using HCLAcademy.Util;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using Microsoft.SharePoint.Client.UserProfiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Serialization;
using System.Text;

namespace HCL.Academy.Web.DAL
{
    public class SharePointDAL : IDAL
    {
        private UserManager CurrentUser;
        private ICredentials SPCredential;
        private string CurrentSiteUrl;
        private ListItemCollection ConfigItems;
        private bool IsOnline;

        public SharePointDAL()
        {
            CurrentSiteUrl = ConfigurationManager.AppSettings["URL"].ToString();
            SPCredential = (ICredentials)HttpContext.Current.Session["SPCredential"];
            CurrentUser = (UserManager)HttpContext.Current.Session["CurrentUser"];
            ConfigItems = (ListItemCollection)HttpContext.Current.Session["AcademyConfig"];
            IsOnline = (bool)HttpContext.Current.Session["IsOnline"];

        }

        #region ########## PUBLIC METHODS ############

        public List<AcademyVideo> GetAllAcademyVideos()
        {
            List<AcademyVideo> lstAcademyVideo = HttpContext.Current.Session[AppConstant.AllVideoData] as List<AcademyVideo>;

            if (lstAcademyVideo == null)
            {
                lstAcademyVideo = new List<AcademyVideo>();
                try
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;

                        string camlQuery = "<Where><Eq><FieldRef Name='ContentType'/><Value Type='Computed'>Video</Value></Eq></Where>";

                        ListItemCollection lstItems = GetWikiDocumentItems(AppConstant.AcademyVideos, context, null, camlQuery);

                        context.Load(lstItems,
                        itms => itms.Include(
                            i => i["Title"],
                            i => i["EncodedAbsUrl"],
                            i => i["VideoSetDescription"],
                            i => i["IsLink"],
                            i => i["Link"]
                            )
                        );

                        context.ExecuteQuery();

                        if (lstItems != null & lstItems.Count > 0)
                        {
                            foreach (ListItem item in lstItems)
                            {
                                AcademyVideo academyVideo = new AcademyVideo();
                                academyVideo.Title = Convert.ToString(item.FieldValues["Title"]);
                                academyVideo.Url = Convert.ToString(item.FieldValues["EncodedAbsUrl"]) + "/" + Convert.ToString(item.FieldValues["Title"]) + ".mp4";
                                academyVideo.Description = Convert.ToString(item.FieldValues["VideoSetDescription"]);
                                academyVideo.IsLink = Convert.ToString(item.FieldValues["IsLink"]);
                                academyVideo.ExternalLink = Convert.ToString(item.FieldValues["Link"]);

                                lstAcademyVideo.Add(academyVideo);
                            }

                            HttpContext.Current.Session[AppConstant.AllVideoData] = lstAcademyVideo;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllAcademyVideos", ex.Message, ex.StackTrace));

                }
            }

            return lstAcademyVideo;
        }

        public FileStreamResult GetVideoStream(string url)
        {
            using (ClientContext context = new ClientContext(CurrentSiteUrl))
            {
                context.Credentials = SPCredential;
                Stream inputStream = null;
                inputStream = (SharePointUtil.GetFile(url, context));
                return new FileStreamResult(inputStream, "video/mp4");
            }
        }

        public Assessments GetAssessmentDetails(int AssessmentId)
        {
            Assessments assessment = new Assessments();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    int assessmentId = AssessmentId;
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
                    List trainingAssessment = context.Web.Lists.GetByTitle(AppConstant.Assessments);
                    ListItem trainingAssessmentItem = trainingAssessment.GetItemById(objAssessment.TrainingAssessmentLookUpId);
                    context.Load(trainingAssessmentItem);
                    context.ExecuteQuery();
                    if (trainingAssessmentItem != null)
                    {
                        assessment.PassingPercentage = Convert.ToInt32(trainingAssessmentItem.FieldValues["PassingMarks"]);
                        assessmentDetails.SingleOrDefault().TrainingAssessmentTimeInMins = Convert.ToInt32(trainingAssessmentItem.FieldValues["AssessmentTimeInMins"]);
                    }
                    var questions = GetEachQuestionDetails(AppConstant.AcademyAssessment, objAssessment.TrainingAssessmentLookUpId);

                    questions = questions.OrderBy(x => Guid.NewGuid()).Take(AppConstant.MaxQueForAssessment).ToList();
                    var totalMarks = questions.Sum(x => Convert.ToInt32(x.Marks));
                    assessment.QuestionDetails = questions;
                    assessment.AssessmentDetails = assessmentDetails;
                    assessment.TotalMarks = totalMarks;
                }
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
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    List userAssessmentList = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <And>
                                                    <Eq>
                                                        <FieldRef Name='User1' LookupId='True' />
                                                        <Value Type='User'>{0}</Value>
                                                    </Eq>
                                                    <Eq>
                                                        <FieldRef Name='Assessment' LookupId='True' />
                                                        <Value Type='Lookup'>{1}</Value>
                                                    </Eq>
                                                </And>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, CurrentUser.SPUserId, result.AssessmentId);
                    ListItemCollection collection = userAssessmentList.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();

                    #region Assessment History

                    DateTime CurrDate = DateTime.Now;
                    List userAssessmentHistoryList = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentHistory);
                    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                    foreach (var item in questionDetails)
                    {
                        ListItem answerItem = userAssessmentHistoryList.AddItem(itemCreateInfo);
                        answerItem["User1"] = CurrentUser.SPUserId;
                        answerItem["Assessment"] = result.AssessmentId;
                        answerItem["Question"] = item.QuestionTitle;
                        answerItem["CorrectOption"] = item.CorrectOption;
                        answerItem["SelectedOption"] = Convert.ToString(item.SelectedOption);
                        answerItem["TimeStamp"] = CurrDate.ToString("dd/MM/yyyy HH:mm");
                        answerItem.Update();
                    }
                    context.ExecuteQuery();

                    #endregion Assessment History    

                    decimal percentage = (result.SecuredMarks * 100) / result.TotalMarks;
                    if (percentage >= Convert.ToDecimal(result.PassingPercentage))
                    {
                        response = true;
                        #region Send certificate
                        Assessments AssessmentItem = (Assessments)HttpContext.Current.Session["StartAssessment"];
                        Hashtable hashtable = new Hashtable();
                        hashtable.Add("UserName", CurrentUser.UserName);
                        hashtable.Add("ClientName", ConfigurationManager.AppSettings["ClientName"].ToString());
                        hashtable.Add("Completed Date", DateTime.Now.ToString("dd.MMM.yyy"));
                        hashtable.Add("AssessmentName", Convert.ToString(AssessmentItem.AssessmentName));
                        hashtable.Add("MarksInPercentage", percentage);
                        bool Queue = SharePointUtil.AddToEmailQueue(context, "SendAssessmentCertificates", hashtable, CurrentUser.EmailID, null);
                        #endregion
                    }

                    if (collection.Count() == 1)
                    {
                        ListItem item = collection.First();
                        item["MarksObtained"] = result.SecuredMarks;
                        item["MarksInPercentage"] = percentage;
                        if (response)
                        {
                            item["CompletedDate"] = DateTime.Now;
                            item["IsAssessmentComplete"] = true;
                            item["CertificateMailSent"] = true;
                        }
                        item.Update();
                        context.ExecuteQuery();

                        /////Assign Points to User if assessment is completed///////////
                        if (response)
                        {
                            /////////////Get Assessment Details///////////////
                            CamlQuery queryAssessment = new CamlQuery();
                            queryAssessment.ViewXml = @"<View><Query>
                                                                <Where>
                                                                    <Eq><FieldRef Name='ID'/>
                                                                        <Value Type='Counter'>{0}</Value>
                                                                    </Eq>                                                                                                    
                                                                </Where>
                                                            </Query>
                                                    </View>";
                            queryAssessment.ViewXml = string.Format(queryAssessment.ViewXml, result.AssessmentId);
                            List lstassessment = context.Web.Lists.GetByTitle(AppConstant.Assessments);
                            ListItemCollection assessmentItems = lstassessment.GetItems(queryAssessment);
                            context.Load(assessmentItems);
                            context.ExecuteQuery();
                            ListItem assessmentItem = assessmentItems.SingleOrDefault();
                            List<Competence> allCompetencyLevelItems = GetAllCompetenceList();

                            if (assessmentItem != null)
                            {
                                int points = Convert.ToInt32(assessmentItem["Points"]);
                                FieldLookupValue userSkill = assessmentItem["Skill"] as FieldLookupValue;
                                FieldLookupValue competenceLevel = assessmentItem["CompetencyLevel"] as FieldLookupValue;
                                List<Competence> querySkillCompetencyLevelItems = allCompetencyLevelItems.Where(c => c.CompetenceId == competenceLevel.LookupId).ToList();


                                ////////////////////Get UserPoint record///////////////////
                                CamlQuery queryUserPoint = new CamlQuery();
                                queryUserPoint.ViewXml = @"<View>
                                        <Query>
                                           <Where>
                                             <And>  
                                              <Eq>
                                                  <FieldRef Name='CompetencyLevel' LookupId='True' />
                                                  <Value Type='Lookup'>{0}</Value>
                                              </Eq>
                                              <And>
                                                 <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{1}</Value>
                                                 </Eq>
                                                 <Eq>
                                                    <FieldRef Name='Skill' LookupId='True' />
                                                    <Value Type='Lookup'>{2}</Value>
                                                 </Eq>
                                              </And>
                                             </And>
                                           </Where>
                                        </Query>
                                      </View>";

                                queryUserPoint.ViewXml = string.Format(queryUserPoint.ViewXml, competenceLevel.LookupId, CurrentUser.SPUserId, userSkill.LookupId);

                                List lstUserPoints = context.Web.Lists.GetByTitle(AppConstant.UserPoints);
                                ListItemCollection userPointItems = lstUserPoints.GetItems(queryUserPoint);
                                context.Load(userPointItems);
                                context.ExecuteQuery();

                                if (userPointItems != null && userPointItems.Count > 0)
                                {
                                    ListItem userPointItem = userPointItems.SingleOrDefault();
                                    if (userPointItem != null)
                                    {

                                        int currentTrainingPoint = 0;
                                        int currentAssessmentPoint = 0;

                                        if (userPointItem["AssessmentPoints"] != null)
                                        {
                                            string assessmentPoints = Convert.ToString(userPointItem["AssessmentPoints"]);
                                            if (assessmentPoints.Length > 0)
                                            {
                                                int currentPoints = Convert.ToInt32(assessmentPoints);
                                                currentPoints = currentPoints + points;
                                                userPointItem["AssessmentPoints"] = currentPoints;
                                                currentAssessmentPoint = currentPoints;
                                            }
                                            else
                                            {
                                                userPointItem["AssessmentPoints"] = points;
                                                currentAssessmentPoint = points;
                                            }
                                        }
                                        else
                                        {
                                            userPointItem["AssessmentPoints"] = points;
                                            currentAssessmentPoint = points;
                                        }
                                        if (userPointItem["TrainingPoints"] != null)
                                        {
                                            currentTrainingPoint = Convert.ToInt32(userPointItem["TrainingPoints"]);
                                        }

                                        bool ifElevate = false;
                                        int competencyLevelOrder = 0;

                                        //////////Check if current competency level is complete////////////
                                        if (querySkillCompetencyLevelItems != null && querySkillCompetencyLevelItems.Count > 0)
                                        {
                                            int trainingCompletionPoints = 0;
                                            int assessmentCompletionPoints = 0;
                                            trainingCompletionPoints = querySkillCompetencyLevelItems[0].TrainingCompletionPoints;

                                            assessmentCompletionPoints = querySkillCompetencyLevelItems[0].AssessmentCompletionPoints;
                                            if (currentAssessmentPoint >= assessmentCompletionPoints && currentTrainingPoint >= trainingCompletionPoints)
                                            {
                                                userPointItem["CompletionStatus"] = true;
                                                competencyLevelOrder = querySkillCompetencyLevelItems[0].CompetencyLevelOrder;
                                                ifElevate = true;
                                            }
                                        }

                                        userPointItem.Update();
                                        context.ExecuteQuery();

                                        /////Elevate the user to next competency level/////
                                        if (ifElevate)
                                        {
                                            List<Competence> skillCompItems = allCompetencyLevelItems.Where(c => c.CompetencyLevelOrder == (competencyLevelOrder + 1) && c.SkillId == userSkill.LookupId).ToList();
                                            if (skillCompItems != null && skillCompItems.Count > 0)
                                            {
                                                ///////Get record for the skill from UserSkills list/////////
                                                CamlQuery queryUserSkill = new CamlQuery();
                                                queryUserSkill.ViewXml = @"<View>
                                             <Query>
                                               <Where>                                                                                                   
                                                  <And>
                                                     <Eq>
                                                        <FieldRef Name='User1' LookupId='True' />
                                                        <Value Type='User'>{0}</Value>
                                                     </Eq>
                                                     <Eq>
                                                        <FieldRef Name='Skill' LookupId='True' />
                                                        <Value Type='Lookup'>{1}</Value>
                                                     </Eq>
                                                  </And>                                                 
                                               </Where>
                                            </Query>
                                           </View>";
                                                queryUserSkill.ViewXml = string.Format(queryUserSkill.ViewXml, CurrentUser.SPUserId, userSkill.LookupId);
                                                List lstUserSkill = context.Web.Lists.GetByTitle(AppConstant.UserSkills);
                                                ListItemCollection userSkillItems = lstUserSkill.GetItems(queryUserSkill);
                                                context.Load(userSkillItems);
                                                context.ExecuteQuery();

                                                ///////Elevate competency level in UserSkills list//////
                                                if (userSkillItems != null && userSkillItems.Count > 0)
                                                {
                                                    ListItem userSkillItem = userSkillItems.SingleOrDefault();
                                                    userSkillItem["CompetencyLevel"] = skillCompItems[0].CompetenceId;
                                                    userSkillItem.Update();
                                                    context.ExecuteQuery();


                                                    ////////Assign training and assessment for the elevated competency level///////////////
                                                    AddSkillBasedTrainingAssessment(skillCompItems[0].CompetenceId.ToString(), Convert.ToInt32(userSkill.LookupId), Convert.ToInt32(CurrentUser.SPUserId));

                                                    ///////Reset UserPoints record for elevated competency level/////////////
                                                    List listUserPoints = context.Web.Lists.GetByTitle(AppConstant.UserPoints);
                                                    ListItemCreationInformation itemCreateInfoUserPoints = new ListItemCreationInformation();
                                                    ListItem listItemUserPoints = listUserPoints.AddItem(itemCreateInfoUserPoints);
                                                    listItemUserPoints["CompetencyLevel"] = skillCompItems[0].CompetenceId.ToString();
                                                    listItemUserPoints["Skill"] = userSkill.LookupId;
                                                    listItemUserPoints["User1"] = CurrentUser.SPUserId;
                                                    listItemUserPoints["TrainingPoints"] = 0;
                                                    listItemUserPoints["AssessmentPoints"] = 0;
                                                    listItemUserPoints.Update();

                                                    context.ExecuteQuery();
                                                }
                                            }
                                        }
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

        public List<Project> GetAllProjects()
        {
            List<Project> projects = new List<Project>();
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    ListItemCollection lstProjects = SharePointUtil.GetListItems(AppConstant.Projects, context, null, null);

                    if (lstProjects != null & lstProjects.Count > 0)
                    {
                        Project proj = null;
                        foreach (ListItem item in lstProjects)
                        {
                            proj = new Project()
                            {
                                ID = Convert.ToInt32(item["ID"]),
                                ProjectName = Convert.ToString(item["Title"])
                            };
                            projects.Add(proj);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllProjects", ex.Message, ex.StackTrace));

            }
            return projects;
        }

        public List<Users> GetUsers()
        {
            List<Users> lstUsers = new List<Users>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    ListItemCollection lstOnBoarding = SharePointUtil.GetListItems(AppConstant.AcademyOnBoarding, context, null, null);

                    if (lstOnBoarding != null & lstOnBoarding.Count > 0)
                    {
                        Users objUser = null;
                        foreach (ListItem item in lstOnBoarding)
                        {
                            objUser = new Users()
                            {
                                userID = ((FieldLookupValue)(item.FieldValues["User1"])).LookupId,
                                userName = ((FieldLookupValue)(item.FieldValues["User1"])).LookupValue,
                                projectName = item["Project"] != null ? ((FieldLookupValue)(item["Project"])).LookupValue : ""
                            };
                            lstUsers.Add(objUser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUsers", ex.Message, ex.StackTrace));

            }

            return lstUsers;

        }

        public void UpdateProjectData(AssignUser objUserOnboard)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List listOnBoard = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                    ListItemCreationInformation itemCreateInfoOnBoard = new ListItemCreationInformation();

                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, objUserOnboard.selectedUser);

                    ListItemCollection listItemOnBoard = listOnBoard.GetItems(query);
                    context.Load(listItemOnBoard);
                    context.ExecuteQuery();
                    ListItem itemOnBoard = listItemOnBoard.SingleOrDefault();
                    itemOnBoard["Project"] = objUserOnboard.selectedProject;
                    itemOnBoard.Update();
                    context.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, UpdateProjectData", ex.Message, ex.StackTrace));

            }

        }

        public List<WikiPolicies> GetAllWikiPolicies()
        {
            List<WikiPolicies> wikiPol = HttpContext.Current.Session[AppConstant.AllWikiPolicyData] as List<WikiPolicies>;
            if (wikiPol == null)
            {
                wikiPol = new List<WikiPolicies>();
                try
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;

                        ListItemCollection lstItemsPolicy = GetPolicyDocumentItems(AppConstant.FAQs, context, null, string.Empty);

                        if (lstItemsPolicy != null & lstItemsPolicy.Count > 0)
                        {
                            foreach (ListItem item in lstItemsPolicy)
                            {
                                WikiPolicies wiki = new WikiPolicies();
                                wiki.DocumentName = item.DisplayName + "." + item["EncodedAbsUrl"].ToString().Split('/').Last().Split('.').Last().ToString();
                                wiki.PolicyOwner = Convert.ToString(item["PolicyOwner"]);
                                wiki.DocumentURL = Convert.ToString(item["EncodedAbsUrl"]);
                                wikiPol.Add(wiki);
                            }
                            HttpContext.Current.Session[AppConstant.AllWikiPolicyData] = wikiPol;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllWikiPolicies", ex.Message, ex.StackTrace));

                }
            }
            return wikiPol;
        }

        public void AddProject(string projectName)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List list = context.Web.Lists.GetByTitle(AppConstant.Projects);
                    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                    ListItem listItem = list.AddItem(itemCreateInfo);

                    if (context != null)
                    {
                        listItem["Title"] = projectName;
                        listItem.Update();
                        context.ExecuteQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AddProject", ex.Message, ex.StackTrace));

            }
        }

        public Project EditProjectByID(int projectID)
        {
            Project project = new Project();
            project.ID = projectID;

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List listProject = context.Web.Lists.GetByTitle(AppConstant.Projects);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                         <Query>
                                             <Where>
                                                 <Eq>
                                                     <FieldRef Name='ID' />
                                                     <Value Type='Counter'>{0}</Value>
                                                 </Eq>
                                             </Where>
                                         </Query>
                                       </View>";

                    query.ViewXml = string.Format(query.ViewXml, project.ID);
                    ListItemCollection listItemCollectionProjects = listProject.GetItems(query);
                    context.Load(listItemCollectionProjects);
                    context.ExecuteQuery();

                    if (listItemCollectionProjects != null && listItemCollectionProjects.Count() > 0)
                    {
                        ListItem listItemProject = listItemCollectionProjects.SingleOrDefault();
                        project.ProjectName = listItemProject["Title"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, EditProjectByID", ex.Message, ex.StackTrace));
            }

            return project;
        }

        public void UpdateProject(Project project)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List listProject = context.Web.Lists.GetByTitle(AppConstant.Projects);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                         <Query>
                                             <Where>
                                                 <Eq>
                                                     <FieldRef Name='ID' />
                                                     <Value Type='Counter'>{0}</Value>
                                                 </Eq>
                                             </Where>
                                         </Query>
                                       </View>";

                    query.ViewXml = string.Format(query.ViewXml, project.ID);
                    ListItemCollection listItemCollectionProjects = listProject.GetItems(query);
                    context.Load(listItemCollectionProjects);
                    context.ExecuteQuery();

                    if (listItemCollectionProjects != null && listItemCollectionProjects.Count() > 0)
                    {
                        ListItem listItemProject = listItemCollectionProjects.SingleOrDefault();
                        listItemProject["Title"] = project.ProjectName;
                        listItemProject.Update();
                        context.ExecuteQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, UpdateProject", ex.Message, ex.StackTrace));

            }
        }

        public void RemoveProject(int projectID)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List listProject = context.Web.Lists.GetByTitle(AppConstant.Projects);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                         <Query>
                                             <Where>
                                                 <Eq>
                                                     <FieldRef Name='ID' />
                                                     <Value Type='Counter'>{0}</Value>
                                                 </Eq>
                                             </Where>
                                         </Query>
                                       </View>";

                    query.ViewXml = string.Format(query.ViewXml, projectID);
                    ListItemCollection listItemCollectionProjects = listProject.GetItems(query);
                    context.Load(listItemCollectionProjects);
                    context.ExecuteQuery();

                    if (listItemCollectionProjects != null && listItemCollectionProjects.Count() > 0)
                    {
                        ListItem listItemProject = listItemCollectionProjects.SingleOrDefault();
                        listItemProject.DeleteObject();
                        context.ExecuteQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, RemoveProject", ex.Message, ex.StackTrace));

            }
        }

        public List<Skills> GetSkills()
        {
            List<Skills> skillSet = new List<Skills>();

            try
            {
                List<Skill> skills = GetAllSkills();
                List<Competence> competencies = GetAllCompetenceList();
                List<SkillCompetencies> trainings = GetSkillCompetencyTraingings();

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

                        skillSet.Add(objSkill);

                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetSkills", ex.Message, ex.StackTrace));

            }
            return skillSet;

        }

        public List<Result> Search(string keyword)
        {
            List<Result> lstResult = null;

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List<Result> lstResult_ = null;
                    lstResult_ = GetSearch(keyword);
                    if (lstResult_ != null && lstResult_.Count > 0)
                    {
                        lstResult = new List<Result>();
                        lstResult = lstResult_;
                    }
                    else
                    {
                        lstResult = new List<Result>();
                        lstResult = null;
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, Search", ex.Message, ex.StackTrace));

            }

            return lstResult;
        }

        public HeatMapProjectDetail GetHeatMapProjectDetailByProjectID(int projectID)
        {
            HeatMapProjectDetail heatMapProjectDetail = new HeatMapProjectDetail();
            heatMapProjectDetail.ID = projectID;
            heatMapProjectDetail.CompetencyLevel = "Novice";

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List lstAssessment = context.Web.Lists.GetByTitle(AppConstant.Projects);
                    ListItem itemProject = lstAssessment.GetItemById(projectID);
                    context.Load(itemProject);
                    context.ExecuteQuery();

                    if (itemProject != null)
                    {
                        heatMapProjectDetail.ProjectName = itemProject["Title"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetHeatMapProjectDetailByProjectID", ex.Message, ex.StackTrace));

            }

            return heatMapProjectDetail;
        }

        public List<SiteMenu> GetMenu()
        {
            List<SiteMenu> siteMenu = null;
            List<int> userRoleList = new List<int>();
            List<SiteMenu> roleBasedsiteMenu = new List<SiteMenu>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    ListItemCollection lstMenuItems = SharePointUtil.GetListItems(
                        AppConstant.SiteMenu, context, null, string.Empty);

                    if (System.Web.HttpContext.Current.Session["UserRole"] == null)
                    {
                        UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                        List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                        CamlQuery query = new CamlQuery();

                        query.ViewXml = @"<View>
                         <Query>
                             <Where>
                                 <Eq>
                                     <FieldRef Name='User1' LookupId='True' />
                                     <Value Type='User'>{0}</Value>
                                 </Eq>
                             </Where>
                         </Query>
                       </View>";
                        query.ViewXml = string.Format(query.ViewXml, users.SPUserId);
                        ListItemCollection onBoardedUsersCollection = academyOnBoarding.GetItems(query);
                        context.Load(onBoardedUsersCollection);
                        context.ExecuteQuery();



                        if (onBoardedUsersCollection != null && onBoardedUsersCollection.Count > 0)
                        {

                            ListItem onBoardedUser = onBoardedUsersCollection.FirstOrDefault();

                            //  var userRolesLookUp = onBoardedUser["Roles"] as FieldLookupValue[];

                            FieldLookupValue[] userRolesLookUp = ((FieldLookupValue[])onBoardedUser["Roles"]);

                            if (userRolesLookUp != null)
                            {
                                foreach (var userRole in userRolesLookUp)
                                {
                                    var userRoleValue = userRole.LookupValue;
                                    int userRoleId = userRole.LookupId;
                                    userRoleList.Add(userRoleId);

                                }
                            }

                        }

                        System.Web.HttpContext.Current.Session["UserRole"] = userRoleList;
                    }

                    siteMenu = GetMenuList(lstMenuItems);

                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetMenu", ex.Message, ex.StackTrace));

            }

            return siteMenu;
        }

        public UserManager GetCurrentUserCompleteUserProfile()
        {
            UserManager user = CurrentUser;

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    PeopleManager peopleManager = new PeopleManager(context);
                    PersonProperties personProperties = peopleManager.GetMyProperties();

                    context.Load(personProperties, p => p.AccountName,
                                                          p => p.DisplayName,
                                                          p => p.UserProfileProperties,
                                                          p => p.DirectReports,
                                                          p => p.ExtendedManagers,
                                                          p => p.Peers,
                                                          p => p.Title);

                    context.ExecuteQuery();

                    string managerName = string.Empty;
                    PersonProperties myManagerpersonProperties;
                    List<string> peersname = new List<string>();
                    List<string> reprteename = new List<string>();
                    if (personProperties.ExtendedManagers != null && personProperties.ExtendedManagers.Count() > 0)
                    {
                        PeopleManager myManager = new PeopleManager(context);
                        myManagerpersonProperties = myManager.GetPropertiesFor(personProperties.ExtendedManagers.Last());
                        context.Load(myManagerpersonProperties);
                        context.ExecuteQuery();
                        if (myManagerpersonProperties != null) managerName = myManagerpersonProperties.DisplayName;


                    }
                    if (personProperties.Peers != null && personProperties.Peers.Count() > 0)
                    {
                        foreach (string peers in personProperties.Peers)//.Take(5))
                        {
                            string peer = peers;
                            PeopleManager peersDetails = new PeopleManager(context);
                            string[] peerParts = peer.Split("|".ToCharArray());
                            peersname.Add(peerParts[peerParts.Length - 1]);
                        }
                    }




                    if (personProperties.DirectReports != null && personProperties.DirectReports.Count() > 0)
                    {
                        foreach (string reportee in personProperties.DirectReports)
                        {
                            string reporteeemail = reportee;
                            string[] reporteeParts = reporteeemail.Split("|".ToCharArray());
                            reprteename.Add(reporteeParts[reporteeParts.Length - 1]);
                        }
                    }
                    user.UserName = personProperties.DisplayName.ToString();
                    user.Designation = personProperties.Title;
                    user.Competency = user.Competency;
                    user.Manager = managerName;
                    //user.Manager = GetManagerDetails(personProperties);
                    // user.Peers = GetPeersDetails(personProperties);
                    user.Peers = peersname;
                    //user.Reportees = GetReporteeDetails(personProperties);
                    user.Reportees = reprteename;
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetCurrentUserCompleteUserProfile", ex.Message, ex.StackTrace));

            }
            return user;
        }

        public List<Banners> GetBanners()
        {
            List<Banners> bannersList = new List<Banners>();

            try
            {
                ListItemCollection lstBannerItems = GetFolderItems(
                ListConstant.SiteAssets, ListConstant.BannerFolder);

                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    var serverUrl = new Uri(context.Url).GetLeftPart(UriPartial.Authority);

                    if (lstBannerItems != null && lstBannerItems.Count > 0)
                    {
                        foreach (ListItem item in lstBannerItems)
                        {
                            Banners banners = new Banners();
                            string strurl = string.Empty;

                            #region Get images from SP Lib
                            strurl = serverUrl + Convert.ToString(item.FieldValues[FieldConstant.FileRef]);
                            Stream stream = (SharePointUtil.GetFile(strurl, context));
                            #endregion

                            banners.BannerImageSRC = Utilities.CreateBase64Image(stream);

                            bannersList.Add(banners);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetBanners", ex.Message, ex.StackTrace));

            }
            return bannersList;
        }

        public string GetBase64BitLogoImageStream()
        {
            string imageStream = null;
            try
            {
                ListItemCollection lstlogoItems = GetFolderItems(
                ListConstant.SiteAssets, ListConstant.LogoFolder);

                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    var serverUrl = new Uri(context.Url).GetLeftPart(UriPartial.Authority);

                    if (lstlogoItems != null && lstlogoItems.Count > 0)
                    {
                        foreach (ListItem item in lstlogoItems)
                        {
                            logos logos = new logos();
                            string strurl = string.Empty;

                            #region Get images from SP Lib
                            strurl = serverUrl + Convert.ToString(item.FieldValues[FieldConstant.FileRef]);
                            Stream stream = (SharePointUtil.GetFile(strurl, context));
                            #endregion

                            imageStream = Utilities.CreateBase64Image(stream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetBase64BitLogoImageStream", ex.Message, ex.StackTrace));

            }

            return imageStream;
        }

        public Resource GetResourceDetailsByProjectID(int projectID)
        {
            Resource resource = new Resource();
            resource.projectId = projectID;
            resource.allResources = new List<AllResources>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    CamlQuery querySkills = new CamlQuery();
                    querySkills.ViewXml = @"<View>
                                         <Query>
                                            <Where>                                               
                                                  <Eq>
                                                     <FieldRef Name='Project' LookupId='True' />
                                                     <Value Type='Lookup'>{0}</Value>
                                                  </Eq>                                               
                                            </Where>
                                         </Query>
                                       </View>";

                    querySkills.ViewXml = string.Format(querySkills.ViewXml, projectID);
                    List listResource = context.Web.Lists.GetByTitle(AppConstant.ProjectSkillResource);
                    ListItemCollection listItemResource = listResource.GetItems(querySkills);
                    context.Load(listItemResource);
                    context.ExecuteQuery();

                    if (listItemResource.Count > 0)
                    {
                        for (int counter = 0; counter < listItemResource.Count; counter++)
                        {
                            FieldLookupValue skill = listItemResource[counter]["Skill"] as FieldLookupValue;
                            FieldLookupValue project = listItemResource[counter]["Project"] as FieldLookupValue;
                            resource.projectName = project.LookupValue;

                            bool flag = false;
                            for (int resCounter = 0; resCounter < resource.allResources.Count; resCounter++)
                            {
                                if (resource.allResources[resCounter].skillId == skill.LookupId)
                                    flag = true;
                            }

                            if (flag == false)
                            {
                                AllResources allResource = new AllResources();
                                allResource.skill = skill.LookupValue;
                                allResource.skillId = skill.LookupId;
                                resource.allResources.Add(allResource);
                            }
                        }

                        for (int counter = 0; counter < listItemResource.Count; counter++)
                        {
                            FieldLookupValue skill = listItemResource[counter]["Skill"] as FieldLookupValue;

                            for (int resCounter = 0; resCounter < resource.allResources.Count; resCounter++)
                            {
                                if (resource.allResources[resCounter].skillId == skill.LookupId)
                                {
                                    FieldLookupValue competencyLevel = listItemResource[counter]["CompetencyLevel"] as FieldLookupValue;
                                    if (competencyLevel.LookupValue.ToUpper() == "BEGINNER" || competencyLevel.LookupValue.ToUpper() == "NOVICE")
                                    {
                                        resource.allResources[resCounter].expectedBeginnerCount = Convert.ToInt32(listItemResource[counter]["ExpectedResourceCount"]);
                                    }
                                    else if (competencyLevel.LookupValue.ToUpper() == "ADVANCED BEGINNER")
                                    {
                                        resource.allResources[resCounter].ExpectedadvancedBeginnerCount = Convert.ToInt32(listItemResource[counter]["ExpectedResourceCount"]);
                                    }
                                    else if (competencyLevel.LookupValue.ToUpper() == "COMPETENT")
                                    {
                                        resource.allResources[resCounter].ExpectedcompetentCount = Convert.ToInt32(listItemResource[counter]["ExpectedResourceCount"]);
                                    }
                                    else if (competencyLevel.LookupValue.ToUpper() == "PROFICIENT")
                                    {
                                        resource.allResources[resCounter].expectedProficientCount = Convert.ToInt32(listItemResource[counter]["ExpectedResourceCount"]);
                                    }
                                    else if (competencyLevel.LookupValue.ToUpper() == "EXPERT")
                                    {
                                        resource.allResources[resCounter].expectedExpertCount = Convert.ToInt32(listItemResource[counter]["ExpectedResourceCount"]);
                                    }

                                    if (competencyLevel.LookupValue.ToUpper() == "BEGINNER" || competencyLevel.LookupValue.ToUpper() == "NOVICE")
                                    {
                                        resource.allResources[resCounter].availableBeginnerCount = Convert.ToInt32(listItemResource[counter]["AvailableResourceCount"]);
                                    }
                                    else if (competencyLevel.LookupValue.ToUpper() == "ADVANCED BEGINNER")
                                    {
                                        resource.allResources[resCounter].AvailableadvancedBeginnerCount = Convert.ToInt32(listItemResource[counter]["AvailableResourceCount"]);
                                    }
                                    else if (competencyLevel.LookupValue.ToUpper() == "COMPETENT")
                                    {
                                        resource.allResources[resCounter].AvailablecompetentCount = Convert.ToInt32(listItemResource[counter]["AvailableResourceCount"]);
                                    }
                                    else if (competencyLevel.LookupValue.ToUpper() == "PROFICIENT")
                                    {
                                        resource.allResources[resCounter].availableProficientCount = Convert.ToInt32(listItemResource[counter]["AvailableResourceCount"]);
                                    }
                                    else if (competencyLevel.LookupValue.ToUpper() == "EXPERT")
                                    {
                                        resource.allResources[resCounter].availableExpertCount = Convert.ToInt32(listItemResource[counter]["AvailableResourceCount"]);
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
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetResourceDetailsByProjectID", ex.Message, ex.StackTrace));

            }

            return resource;
        }

        public List<Skill> GetAllSkills()
        {
            List<Skill> skills = HttpContext.Current.Session[AppConstant.AllSkillData] as List<Skill>;

            if (skills == null)
            {
                skills = new List<Skill>();
                try
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;
                        ListItemCollection lstSkill = SharePointUtil.GetListItems(AppConstant.Skills, context, null, null);

                        if (lstSkill != null & lstSkill.Count > 0)
                        {
                            Skill skill = null;
                            foreach (ListItem item in lstSkill)
                            {
                                if (item["IsDefault"] != null)
                                {
                                    if (item["IsDefault"].ToString().ToUpper() == "FALSE")
                                    {
                                        skill = new Skill()
                                        {
                                            SkillId = item.Id,
                                            SkillName = Convert.ToString(item["Title"])
                                        };
                                        skills.Add(skill);

                                    }

                                }
                                else
                                {
                                    skill = new Skill()
                                    {
                                        SkillId = item.Id,
                                        SkillName = Convert.ToString(item["Title"])
                                    };
                                    skills.Add(skill);
                                }
                            }
                            HttpContext.Current.Session[AppConstant.AllSkillData] = skills;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkills", ex.Message, ex.StackTrace));

                }
            }

            return skills;
        }

        public ProjectDetails GetProjectSkillsByProjectID(string projectID)
        {
            ProjectDetails objProjectDetails = new ProjectDetails();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List projectList = context.Web.Lists.GetByTitle(AppConstant.Projects);
                    CamlQuery queryP = new CamlQuery();
                    queryP.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='ID' />
                                                    <Value Type='Counter'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";

                    queryP.ViewXml = string.Format(queryP.ViewXml, Convert.ToInt32(projectID));
                    ListItemCollection projectscollection = projectList.GetItems(queryP);
                    context.Load(projectscollection);
                    context.ExecuteQuery();
                    ListItem itemProject = projectscollection.SingleOrDefault();
                    objProjectDetails.ProjectId = itemProject.Id;
                    objProjectDetails.ProjectName = itemProject["Title"].ToString();

                    List projectSkillsList = context.Web.Lists.GetByTitle(AppConstant.ProjectSkills);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                    <Query>
                                        <Where>                                           
                                            
                                                <Eq>
                                                    <FieldRef Name='Project' LookupId='True' />
                                                    <Value Type='Lookup'>{0}</Value>
                                                </Eq>                                              
                                        </Where>
                                    </Query>
                            </View>";
                    query.ViewXml = string.Format(query.ViewXml, Convert.ToInt32(projectID));
                    ListItemCollection collection = projectSkillsList.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();

                    objProjectDetails.ProjectSkill = new List<ProjectSkill>();
                    ProjectSkill objProjectSkill = null;
                    if (collection != null && collection.Count > 0)
                    {
                        foreach (var item in collection)
                        {
                            objProjectSkill = new ProjectSkill();

                            objProjectSkill.ItemId = item.Id;
                            objProjectSkill.Project = item["Project"] != null ? ((FieldLookupValue)(item["Project"])).LookupValue : "";
                            objProjectSkill.ProjectId = item["Project"] != null ? ((FieldLookupValue)(item["Project"])).LookupId : -1;
                            objProjectSkill.Skill = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "";
                            objProjectSkill.SkillId = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupId : -1;

                            objProjectDetails.ProjectSkill.Add(objProjectSkill);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetProjectSkillsByProjectID", ex.Message, ex.StackTrace));

            }

            return objProjectDetails;
        }

        public ProjectSkill PostProjectSkill(string projectid, string skillid)
        {
            ProjectSkill objProjectSkill = new ProjectSkill();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;


                    List list = context.Web.Lists.GetByTitle(AppConstant.ProjectSkills);
                    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                    ListItem listItem = list.AddItem(itemCreateInfo);
                    listItem["Project"] = Convert.ToInt32(projectid);
                    listItem["Skill"] = Convert.ToInt32(skillid);

                    listItem.Update();
                    context.Load(listItem);
                    context.ExecuteQuery();
                    objProjectSkill.ItemId = listItem.Id;
                    objProjectSkill.Project = listItem["Project"] != null ? ((FieldLookupValue)(listItem["Project"])).LookupValue : "";
                    objProjectSkill.ProjectId = listItem["Project"] != null ? ((FieldLookupValue)(listItem["Project"])).LookupId : -1;
                    objProjectSkill.Skill = listItem["Skill"] != null ? ((FieldLookupValue)(listItem["Skill"])).LookupValue : "";
                    objProjectSkill.SkillId = listItem["Skill"] != null ? ((FieldLookupValue)(listItem["Skill"])).LookupId : -1;

                    // Get competency levels for the selected skill from Skill
                    List<Competence> skillCompetencies = GetCompetenciesForASkill(skillid);
                    // for each level insert one row into the list 'ProjectSkillresource'

                    List listSkillResource = context.Web.Lists.GetByTitle(AppConstant.ProjectSkillResource);

                    foreach (Competence item in skillCompetencies)
                    {
                        ListItemCreationInformation listItemCreateInfo = new ListItemCreationInformation();
                        ListItem oListItem = listSkillResource.AddItem(listItemCreateInfo);

                        oListItem["Project"] = Convert.ToInt32(projectid);
                        oListItem["Skill"] = Convert.ToInt32(skillid);
                        oListItem["CompetencyLevel"] = item.CompetenceId;
                        oListItem["ExpectedResourceCount"] = 0;
                        oListItem["AvailableResourceCount"] = 0;
                        oListItem.Update();
                    }

                    context.ExecuteQuery();

                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, PostProjectSkill", ex.Message, ex.StackTrace));

            }

            return objProjectSkill;
        }

        public void DeleteProjectSkill(int projectskillid, string projectid, string skillid)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    SharePointUtil.DeteleItemById(context, projectskillid, AppConstant.ProjectSkills);

                    string query = @"<View>
                                        <Query>
                                            <Where>
                                                <And> 
                                                    <Eq>
                                                        <FieldRef Name='Project' LookupId='True' />
                                                        <Value Type='Lookup'>{0}</Value>
                                                    </Eq>                                              
                                                    <Eq>
                                                        <FieldRef Name='Skill' LookupId='True' />
                                                        <Value Type='Lookup'>{1}</Value>
                                                    </Eq>
                                                </And>
                                            </Where>
                                        </Query>
                                      </View>";

                    query = string.Format(query, Convert.ToInt32(projectid), Convert.ToInt32(skillid));

                    ListItemCollection lstSkillResource = SharePointUtil.GetListItems(AppConstant.ProjectSkillResource, context, null, query);

                    if (lstSkillResource != null & lstSkillResource.Count > 0)
                    {
                        foreach (ListItem item in lstSkillResource)
                        {
                            SharePointUtil.DeteleItemById(context, item.Id, AppConstant.ProjectSkillResource);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, DeleteProjectSkill", ex.Message, ex.StackTrace));

            }
        }

        public WikiPolicyDocuments GetWikiPolicyDocuments()
        {
            WikiPolicyDocuments poldocs = new WikiPolicyDocuments();

            try
            {

                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List<WikiPolicies> wikiPol = new List<WikiPolicies>();
                    ListItemCollection lstItemsPolicy = GetPolicyDocumentItems(AppConstant.FAQs, context, null, string.Empty);
                    if (lstItemsPolicy != null & lstItemsPolicy.Count > 0)
                    {
                        foreach (ListItem item in lstItemsPolicy)
                        {
                            WikiPolicies wiki = new WikiPolicies();
                            wiki.DocumentName = item.DisplayName + "." + item["EncodedAbsUrl"].ToString().Split('/').Last().Split('.').Last().ToString();
                            wiki.PolicyOwner = item["PolicyOwner"].ToString();
                            wiki.DocumentURL = item["EncodedAbsUrl"].ToString();
                            wikiPol.Add(wiki);
                        }
                    }
                    poldocs.ListOfWiki = wikiPol;
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetWikiPolicyDocuments", ex.Message, ex.StackTrace));

            }

            return poldocs;
        }

        public Stream DownloadDocument(string decryptFileName)
        {
            Stream fileBytes = null;

            //using (ClientContext context = new ClientContext(CurrentSiteUrl))
            //{
            ClientContext context = new ClientContext(CurrentSiteUrl);
            context.Credentials = SPCredential;

            fileBytes = SharePointUtil.GetFile(decryptFileName, context);
            //}

            return fileBytes;
        }

        public List<WikiDocuments> GetWikiDocumentTree(HttpServerUtilityBase Server)
        {
            List<WikiDocuments> listOfWikiDoc = new List<WikiDocuments>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    string camlQuery = "@<View Scope='RecursiveAll'></View>";
                    ListItemCollection lstItemsDoc = GetWikiDocumentItems(AppConstant.WikiDocuments, context, null, camlQuery);
                    if (lstItemsDoc != null & lstItemsDoc.Count > 0)
                    {
                        List<WikiDocuments> TrainingDocFirst = new List<WikiDocuments>();
                        foreach (ListItem item in lstItemsDoc)
                        {
                            var perms = item.EffectiveBasePermissions.Has(PermissionKind.OpenItems); //Permission Trimmed
                            if (perms)
                            {
                                WikiDocuments wiki = new WikiDocuments();
                                wiki.ID = item.Id;
                                wiki.ParentFolder = Server.UrlDecode(item["EncodedAbsUrl"].ToString().Split('/')[item["EncodedAbsUrl"].ToString().Split('/').Length - 2]);
                                wiki.DocumentURL = item["EncodedAbsUrl"].ToString();
                                wiki.ParentFolderURL = wiki.DocumentURL.Remove(wiki.DocumentURL.LastIndexOf("/"));
                                if (item.FileSystemObjectType == FileSystemObjectType.Folder)
                                {
                                    wiki.IsFolder = true;
                                    wiki.DocumentName = item.DisplayName;
                                }
                                else
                                {
                                    wiki.DocumentName = item.DisplayName + "." + item["EncodedAbsUrl"].ToString().Split('/').Last().Split('.').Last().ToString();
                                    wiki.IsFolder = false;
                                }

                                TrainingDocFirst.Add(wiki);
                            }
                        }
                        listOfWikiDoc = GetChild(TrainingDocFirst); //ReStructure Objects
                    }

                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetWikiDocumentTree", ex.Message, ex.StackTrace));

            }
            return listOfWikiDoc;
        }

        public TrainingReport GetTrainingsReport(string skillid, string competencyid)
        {
            TrainingReport trainingReport = new TrainingReport();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List<UserDetails> usersList = new List<UserDetails>();

                    trainingReport.userDetails = GetUserDetails(context, Convert.ToInt32(skillid), Convert.ToInt32(competencyid));
                    trainingReport.counts = GetTrainingCounts(trainingReport.userDetails);
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetTrainingsReport", ex.Message, ex.StackTrace));

            }
            return trainingReport;
        }

        public List<RSSFeed> GetRSSFeeds()
        {
            string camlQueryRssFeeds = @"<Query>
                                               <OrderBy>
                                                  <FieldRef Name='ID' />
                                               </OrderBy>
                                            </Query>";
            List<RSSFeed> postRSList = new List<RSSFeed>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    ListItemCollection lstItems = SharePointUtil.GetListItems(
                        AppConstant.RSSFeeds, context, null, camlQueryRssFeeds);
                    IEnumerable<ListItem> filteredItems = lstItems.AsEnumerable().OrderBy(x => x.FieldValues["RssFeedOrder"]);

                    if (filteredItems != null & filteredItems.Count() > 0)
                    {
                        foreach (ListItem item in filteredItems)
                        {
                            XmlDocument rssXmlDoc = new XmlDocument();



                            // Load the RSS file from the RSS URL
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

                                postRSList.Add(rs);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetRSSFeeds", ex.Message, ex.StackTrace));

            }

            return postRSList;
        }

        public List<News> GetNews(string NoImagePath)
        {

            List<News> annclst = new List<News>();
            try
            {
                ListItemCollection configItems = ConfigItems;

                var Announcement = from config in configItems
                                   where Convert.ToString(config["Title"]) == "News"
                                   select Convert.ToString(config["Value1"]);

                string camlQueryAnnouncement = @"<View>
                                                <Query>
                                                     <Where>
                                                        <And>
                                                            <Geq>
                                                                    <FieldRef Name='Expires' />
                                                                    <Value Type='DateTime'>
                                                                        <Today OffsetDays='" + Convert.ToString(Announcement) + @"' />
                                                                    </Value>
                                                             </Geq>
                                                              <Geq>
                                                                    <FieldRef Name='Expires' />
                                                                    <Value Type='DateTime'>
                                                                        <Today OffsetDays='0' />
                                                                    </Value>
                                                               </Geq>
                                                         </And>
                                                      </Where>
                                                      <OrderBy>
                                                            <FieldRef Name='Expires' Ascending='TRUE'/>
                                                      </OrderBy>
                                                 </Query>
                                                 <ViewFields>
                                                        <FieldRef Name='Title' />
                                                        <FieldRef Name='Body' />
                                                        <FieldRef Name='Image' />
                                                 </ViewFields>
                                            </View>";

                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    ListItemCollection lstItems = SharePointUtil.GetListItems(
                        AppConstant.AcademyNews, context, null, camlQueryAnnouncement);

                    if (lstItems != null & lstItems.Count > 0)
                    {
                        string strurl = string.Empty;
                        Stream stream = null;
                        foreach (ListItem item in lstItems)
                        {
                            News announce = new News();
                            announce.Header = item["Title"].ToString();
                            announce.Body = item["Body"].ToString();

                            //if (((FieldUrlValue)(item["Image"])) != null)
                            //{
                            //    strurl = ((FieldUrlValue)(item["Image"])).Url;

                            //    stream = (SharePointUtil.GetFile(strurl, context));

                            //    announce.ImageURL = Utilities.CreateBase64Image(stream);
                            //}
                            //else
                            //{
                            string filePath = NoImagePath;
                            FileStream imagestream = System.IO.File.OpenRead(filePath);
                            announce.ImageURL = Utilities.CreateBase64Image(imagestream);
                            // }

                            announce.TrimmedBody = Utilities.Truncate(announce.Body, 25);
                            annclst.Add(announce);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetNews", ex.Message, ex.StackTrace));

            }
            return annclst;
        }

        public List<AcademyEvent> GetEvents()
        {
            List<AcademyEvent> events = HttpContext.Current.Session[AppConstant.AllEventsData] as List<AcademyEvent>;
            if (events == null)
            {
                events = new List<AcademyEvent>();

                try
                {
                    string offsetDays = string.Empty;
                    ListItemCollection items = ConfigItems;

                    foreach (ListItem item in items)
                    {
                        if (item["Title"].Equals("Events"))
                        {
                            offsetDays = item["Value1"].ToString();
                            break;
                        }
                    }

                    string camlQueryEvents = @"<View>
                                        <Query>
                                            <Where>
                                                <Geq>
                                                    <FieldRef Name='EndDate' />
                                                    <Value Type='DateTime' >
                                                        <Today OffsetDays='" + offsetDays + @"' />
                                                    </Value>
                                                </Geq>
                                            </Where>
                                        </Query>
                                        <OrderBy>
                                            <FieldRef Name='EventDate' Ascending='FALSE'/>
                                        </OrderBy>
                                    </View>";

                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;

                        ListItemCollection lstItemsEvents = SharePointUtil.GetListItems(
                            AppConstant.AcademyEvents, context, null, camlQueryEvents);

                        foreach (var item in lstItemsEvents)
                        {
                            string title = item["Title"].ToString();
                            DateTime eventDate = Convert.ToDateTime(item["EventDate"]).ToUniversalTime();
                            string location = item["Location"].ToString();
                            string description = item["Description"].ToString();

                            AcademyEvent event_ = new AcademyEvent()
                            {
                                Title = title,
                                EventDate = eventDate,
                                Location = location,
                                Description = description
                            };

                            events.Add(event_);
                        }
                        HttpContext.Current.Session[AppConstant.AllEventsData] = events;
                    }
                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetEvents", ex.Message, ex.StackTrace));

                }
            }
            return events;
        }

        public List<TrainingPlan> GetTrainingPlans(int id)
        {
            List<TrainingPlan> trainingPlans = new List<TrainingPlan>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    string camlQuery = "@<View Scope='RecursiveAll'></View>";
                    ListItemCollection lstItemsDoc = GetTrainingTree(
                        "Training Plan", context, null, camlQuery);

                    IEnumerable<ListItem> filteredItems = lstItemsDoc.AsEnumerable().Where(x => x.Id == id);
                    if (filteredItems != null & filteredItems.Count() > 0)
                    {
                        foreach (ListItem item in filteredItems)
                        {
                            if (item.Id == id)
                            {
                                TrainingPlan Training = new TrainingPlan();

                                Training.Id = item.Id;
                                Training.Title = Convert.ToString(item["Title"]); ;

                                Training.ContentBody = HtmlToPlainText(Convert.ToString(item["ContentBody"]));

                                if (Training.ContentBody == null || Training.ContentBody == string.Empty || Training.ContentBody == "")
                                {
                                    Training.ContentUrl = ((FieldUrlValue)(item["ContentUrl"])).Url;
                                    Training.ContentBody = String.Empty;
                                }
                                else
                                {
                                    Training.ContentUrl = String.Empty;
                                    Training.ContentBody = Convert.ToString(item["ContentBody"]);
                                }

                                trainingPlans.Add(Training);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetTrainingPlans", ex.Message, ex.StackTrace));

            }

            return trainingPlans;
        }

        public List<UserTraining> GetTrainingForUser(int userId, bool OnlyOnBoardedTraining = false)
        {
            List<UserTraining> lstTrainings = new List<UserTraining>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                    CamlQuery query = new CamlQuery();

                    if (OnlyOnBoardedTraining)
                    {
                        query.ViewXml = @"<View>
                                    <Query>
                                        <Where>                                           
                                            <And>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                                    <Eq>
                                                         <FieldRef Name='IsIncludeOnBoarding' />
                                                         <Value Type='Boolean'>{1}</Value>
                                                    </Eq>                                                    
                                                </And>                                            
                                        </Where>
                                    </Query>
                            </View>";
                        query.ViewXml = string.Format(query.ViewXml, userId, 1);
                    }
                    else
                    {
                        query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                        query.ViewXml = string.Format(query.ViewXml, userId);
                    }

                    ListItemCollection collection = academyOnBoarding.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();

                    UserTraining objTraining = null;
                    if (collection != null && collection.Count > 0)
                    {
                        foreach (var item in collection)
                        {
                            objTraining = new UserTraining();

                            objTraining.CompletedDate = Convert.ToString(item["CompletedDate"]);
                            objTraining.IsIncludeOnBoarding = Convert.ToBoolean(item["IsIncludeOnBoarding"]);
                            objTraining.IsMandatory = Convert.ToBoolean(item["IsMandatory"]);
                            objTraining.IsTrainingActive = Convert.ToBoolean(item["IsTrainingActive"]);
                            objTraining.IsTrainingCompleted = Convert.ToBoolean(item["IsTrainingCompleted"]);
                            objTraining.LastDayCompletion = Convert.ToDateTime(item["LastDayCompletion"].ToString()).ToShortDateString();
                            objTraining.SkillName = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "";
                            objTraining.SkillId = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupId : 0;
                            objTraining.TrainingName = item["Training"] != null ? ((FieldLookupValue)(item["Training"])).LookupValue : "";
                            objTraining.TrainingId= item["Training"] != null ? ((FieldLookupValue)(item["Training"])).LookupId : 0;

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
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetTrainingForUser", ex.Message, ex.StackTrace));

            }

            return lstTrainings;
        }

        public List<OnBoarding> GetBoardingData()
        {
            List<OnBoarding> boardingList = new List<OnBoarding>();
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    List academyJoinersTraining = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                    CamlQuery q = new CamlQuery();
                    q.ViewXml = @"<View>
                                    <Query>
                                        <Where>                                           
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                                <And>
                                                    <Eq>
                                                         <FieldRef Name='IsIncludeOnBoarding' />
                                                         <Value Type='Boolean'>{1}</Value>
                                                    </Eq>                                                    
                                                </And>                                            
                                        </Where>
                                    </Query>
                            </View>";
                    q.ViewXml = string.Format(q.ViewXml, CurrentUser.SPUserId, 1);
                    ListItemCollection joinersTrainingCollection = academyJoinersTraining.GetItems(q);
                    context.Load(joinersTrainingCollection);
                    // Role Training Collection
                    List academyUserRoleTraining = context.Web.Lists.GetByTitle(AppConstant.UserRoleTraining);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                    <Query>
                                        <Where>                                           
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>                                                                                           
                                        </Where>
                                    </Query>
                            </View>";
                    query.ViewXml = string.Format(query.ViewXml, CurrentUser.SPUserId);
                    ListItemCollection userRoleTrainingCollection = academyUserRoleTraining.GetItems(query);
                    context.Load(joinersTrainingCollection);
                    context.Load(userRoleTrainingCollection);
                    context.ExecuteQuery();

                    if (joinersTrainingCollection != null && joinersTrainingCollection.Count() > 0)
                    {
                        foreach (ListItem joinersItem in joinersTrainingCollection)
                        {
                            if ((FieldLookupValue)joinersItem["Training"] != null)
                            {
                                FieldLookupValue traininglookupValue = (FieldLookupValue)joinersItem["Training"];
                                bool trainingStatus = false;

                                if (joinersItem.FieldValues["IsTrainingCompleted"] != null && Convert.ToBoolean(joinersItem["IsTrainingCompleted"]) == true)
                                {
                                    trainingStatus = true;
                                }
                                HCLAcademy.Models.TraningStatus status = Utilities.GetTraningStatus(trainingStatus, 0, Convert.ToDateTime(joinersItem["LastDayCompletion"]));
                                boardingList.Add(new OnBoarding
                                {
                                    BoardingItemId = Convert.ToInt32(joinersItem.FieldValues["ID"]),
                                    BoardingItemName = traininglookupValue.LookupValue,
                                    BoardingStatus = Utilities.GetOnBoardingStatus(trainingStatus, 0, Convert.ToDateTime(joinersItem["LastDayCompletion"])),
                                    BoardingType = OnboardingItemType.Training,
                                    BoardIngTrainingId = traininglookupValue.LookupId
                                });
                            }
                        }
                    }

                    /* Role Training */
                    if (userRoleTrainingCollection != null && userRoleTrainingCollection.Count() > 0)
                    {
                        foreach (ListItem roleTrainingItem in userRoleTrainingCollection)
                        {
                            if ((FieldLookupValue)roleTrainingItem["RoleBasedTraining"] != null)
                            {
                                FieldLookupValue roleTraininglookupValue = (FieldLookupValue)roleTrainingItem["RoleBasedTraining"];
                                bool roleTrainingStatus = false;

                                if (roleTrainingItem.FieldValues["IsTrainingCompleted"] != null && Convert.ToBoolean(roleTrainingItem["IsTrainingCompleted"]) == true)
                                {
                                    roleTrainingStatus = true;
                                }
                                HCLAcademy.Models.TraningStatus status = Utilities.GetTraningStatus(roleTrainingStatus, 0, Convert.ToDateTime(roleTrainingItem["LastDayCompletion"]));
                                boardingList.Add(new OnBoarding
                                {
                                    BoardingItemId = Convert.ToInt32(roleTrainingItem.FieldValues["ID"]),
                                    BoardingItemName = roleTraininglookupValue.LookupValue,
                                    BoardingStatus = Utilities.GetOnBoardingStatus(roleTrainingStatus, 0, Convert.ToDateTime(roleTrainingItem["LastDayCompletion"])),
                                    BoardingType = OnboardingItemType.RoleTraining,
                                    BoardIngTrainingId = roleTraininglookupValue.LookupId
                                });
                            }
                        }
                    }
                    /* Role Training */
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetBoardingData", ex.Message, ex.StackTrace));

            }
            return boardingList;
        }

        public List<UserSkill> GetUserSkillsOfCurrentUser()
        {
            List<UserSkill> lstSkills = new List<UserSkill>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List userSkills = context.Web.Lists.GetByTitle(AppConstant.UserSkills);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, CurrentUser.SPUserId);


                    ListItemCollection collection = userSkills.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();

                    UserSkill objSkill = null;
                    if (collection != null && collection.Count > 0)
                    {
                        foreach (var item in collection)
                        {
                            if (item["CompetencyLevel"] != null)
                            {
                                objSkill = new UserSkill();
                                objSkill.Id = item.Id;
                                objSkill.Skill = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "";
                                objSkill.Competence = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupValue.ToUpper() : "";
                                lstSkills.Add(objSkill);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserSkillsOfCurrentUser", ex.Message, ex.StackTrace));

            }

            return lstSkills;
        }

        public List<Competence> GetAllCompetenceList()
        {
            List<Competence> competenceList = HttpContext.Current.Session[AppConstant.AllCompetencyData] as List<Competence>;

            if (competenceList == null)
            {
                competenceList = new List<Competence>();
                try
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;
                        ListItemCollection lstCompetencyLevel = SharePointUtil.GetListItems(AppConstant.SkillCompetencyLevels, context, null, null);

                        if (lstCompetencyLevel != null & lstCompetencyLevel.Count > 0)
                        {
                            Competence competence = null;
                            foreach (ListItem item in lstCompetencyLevel)
                            {
                                FieldLookupValue userSkill = item["Skill"] as FieldLookupValue;
                                competence = new Competence()
                                {
                                    CompetenceId = item.Id,
                                    CompetenceName = Convert.ToString(item["Title"]),
                                    SkillId = userSkill.LookupId,
                                    SkillName = userSkill.LookupValue,
                                    Description = Convert.ToString(item["Description1"])
                                };
                                competenceList.Add(competence);
                            }
                            HttpContext.Current.Session[AppConstant.AllCompetencyData] = competenceList;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllCompetenceList", ex.Message, ex.StackTrace));

                }

            }

            return competenceList;
        }

        public List<ProjectSkill> GetAllProjectSkills()
        {
            List<ProjectSkill> projectSkills = new List<ProjectSkill>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    ListItemCollection lstProjectSkills = SharePointUtil.GetListItems(AppConstant.ProjectSkills, context, null, null);

                    if (lstProjectSkills != null & lstProjectSkills.Count > 0)
                    {
                        ProjectSkill projectSkill = null;
                        foreach (ListItem item in lstProjectSkills)
                        {
                            projectSkill = new ProjectSkill()
                            {
                                ItemId = Convert.ToInt32(item["ID"]),
                                Project = item["Project"] != null ? ((FieldLookupValue)(item["Project"])).LookupValue : "",
                                ProjectId = item["Project"] != null ? ((FieldLookupValue)(item["Project"])).LookupId : -1,
                                Skill = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "",
                                SkillId = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupId : -1
                            };
                            projectSkills.Add(projectSkill);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllProjectSkills", ex.Message, ex.StackTrace));
            }

            return projectSkills;
        }

        public void AddProjectSkill(int ProjectID, int SkillID)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List listProjectSkill = context.Web.Lists.GetByTitle(AppConstant.ProjectSkills);
                    ListItemCreationInformation itemProjectSkillCreateInfo = new ListItemCreationInformation();
                    ListItem listItemProjectSkill = listProjectSkill.AddItem(itemProjectSkillCreateInfo);
                    listItemProjectSkill["Project"] = ProjectID;
                    listItemProjectSkill["Skill"] = SkillID;
                    listItemProjectSkill.Update();
                    context.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AddProjectSkill", ex.Message, ex.StackTrace));
            }
        }

        public List<ProjectSkillResource> GetAllProjectSkillResources()
        {
            List<ProjectSkillResource> projectSkillResources = new List<ProjectSkillResource>();

            try
            {

                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    ListItemCollection lstProjectSkillResources = SharePointUtil.GetListItems(AppConstant.ProjectSkillResource, context, null, null);

                    if (lstProjectSkillResources != null & lstProjectSkillResources.Count > 0)
                    {
                        ProjectSkillResource projectSkillResource = null;
                        foreach (ListItem item in lstProjectSkillResources)
                        {
                            projectSkillResource = new ProjectSkillResource()
                            {
                                ProjectName = item["Project"] != null ? ((FieldLookupValue)(item["Project"])).LookupValue : "",
                                ProjectId = item["Project"] != null ? ((FieldLookupValue)(item["Project"])).LookupId : -1,
                                Skill = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "",
                                SkillId = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupId : -1,
                                CompetencyLevel = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupValue : "",
                                CompetencyLevelId = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupId : -1,
                                ExpectedResourceCount = Convert.ToString(item["ExpectedResourceCount"]),
                                AvailableResourceCount = Convert.ToString(item["AvailableResourceCount"])

                            };
                            projectSkillResources.Add(projectSkillResource);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllProjectSkillResources", ex.Message, ex.StackTrace));
            }

            return projectSkillResources;
        }

        public void AddProjectSkillResource(int ProjectID, ProjectSkillResource skillResource)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List listProjectSkillResource = context.Web.Lists.GetByTitle(AppConstant.ProjectSkillResource);
                    ListItemCreationInformation itemProjectSkillResourceCreateInfo = new ListItemCreationInformation();
                    ListItem listItemProjectSkillResource = listProjectSkillResource.AddItem(itemProjectSkillResourceCreateInfo);
                    // Add project to Project list
                    listItemProjectSkillResource["Project"] = ProjectID;
                    listItemProjectSkillResource["Skill"] = skillResource.SkillId;
                    listItemProjectSkillResource["CompetencyLevel"] = skillResource.CompetencyLevelId;
                    listItemProjectSkillResource["ExpectedResourceCount"] = skillResource.ExpectedResourceCount;
                    listItemProjectSkillResource["AvailableResourceCount"] = skillResource.AvailableResourceCount;
                    listItemProjectSkillResource.Update();
                    context.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AddProjectSkillResource", ex.Message, ex.StackTrace));

            }
        }

        public void AddProjectSkillResources(ProjectResources prjRes)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    for (int counter = 0; counter < prjRes.skillResources.Count; counter++)
                    {
                        CamlQuery queryResource = new CamlQuery();
                        queryResource.ViewXml = @"<View>
                                         <Query>
                                            <Where>
                                               <And>
                                                  <Eq>
                                                     <FieldRef Name='Project' LookupId='True' />
                                                     <Value Type='Lookup'>{0}</Value>
                                                  </Eq>
                                                  <Eq>
                                                     <FieldRef Name='Skill' LookupId='True' />
                                                     <Value Type='Lookup'>{1}</Value>
                                                  </Eq>
                                               </And>
                                            </Where>
                                         </Query>
                                       </View>";
                        queryResource.ViewXml = string.Format(queryResource.ViewXml, prjRes.projectId, prjRes.skillResources[counter].skillId);

                        List listResource = context.Web.Lists.GetByTitle(AppConstant.ProjectSkillResource);
                        ListItemCollection listItemResource = listResource.GetItems(queryResource);
                        context.Load(listItemResource);
                        context.ExecuteQuery();

                        if (listItemResource.Count > 0)
                        {
                            for (int innerCounter = 0; innerCounter < listItemResource.Count; innerCounter++)
                            {
                                FieldLookupValue competencyLevel = listItemResource[innerCounter]["CompetencyLevel"] as FieldLookupValue;
                                if (competencyLevel.LookupValue.ToUpper() == "BEGINNER" || competencyLevel.LookupValue.ToUpper() == "NOVICE")
                                {
                                    listItemResource[innerCounter]["AvailableResourceCount"] = prjRes.skillResources[counter].beginnerCount;
                                }
                                else if (competencyLevel.LookupValue.ToUpper() == "ADVANCED BEGINNER")
                                {
                                    listItemResource[innerCounter]["AvailableResourceCount"] = prjRes.skillResources[counter].advancedBeginnerCount;
                                }
                                else if (competencyLevel.LookupValue.ToUpper() == "COMPETENT")
                                {
                                    listItemResource[innerCounter]["AvailableResourceCount"] = prjRes.skillResources[counter].competentCount;
                                }
                                else if (competencyLevel.LookupValue.ToUpper() == "PROFICIENT")
                                {
                                    listItemResource[innerCounter]["AvailableResourceCount"] = prjRes.skillResources[counter].proficientCount;
                                }
                                else if (competencyLevel.LookupValue.ToUpper() == "EXPERT")
                                {
                                    listItemResource[innerCounter]["AvailableResourceCount"] = prjRes.skillResources[counter].expertCount;
                                }

                                listItemResource[innerCounter].Update();
                            }
                            context.ExecuteQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AddProjectSkillResources", ex.Message, ex.StackTrace));

            }
        }

        public List<ProjectSkillResource> GetAllProjectSkillResourcesByProjectID(int ProjectID)
        {
            List<ProjectSkillResource> projectSkillResources = new List<ProjectSkillResource>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    CamlQuery querySkills = new CamlQuery();
                    querySkills.ViewXml = @"<View>
                                         <Query>
                                            <Where>                                               
                                                  <Eq>
                                                     <FieldRef Name='Project' LookupId='True' />
                                                     <Value Type='Lookup'>{0}</Value>
                                                  </Eq>                                               
                                            </Where>
                                         </Query>
                                       </View>";
                    querySkills.ViewXml = string.Format(querySkills.ViewXml, ProjectID);
                    List listResource = context.Web.Lists.GetByTitle(AppConstant.ProjectSkillResource);
                    ListItemCollection listItemResource = listResource.GetItems(querySkills);
                    context.Load(listItemResource);
                    context.ExecuteQuery();

                    if (listItemResource.Count > 0)
                    {
                        for (int counter = 0; counter < listItemResource.Count; counter++)
                        {
                            FieldLookupValue skill = listItemResource[counter]["Skill"] as FieldLookupValue;
                            FieldLookupValue project = listItemResource[counter]["Project"] as FieldLookupValue;
                            FieldLookupValue competencyLevel = listItemResource[counter]["CompetencyLevel"] as FieldLookupValue;

                            ProjectSkillResource projectSkillResource = new ProjectSkillResource()
                            {
                                AvailableResourceCount = listItemResource[counter]["AvailableResourceCount"].ToString(),
                                CompetencyLevel = competencyLevel.LookupValue,
                                CompetencyLevelId = competencyLevel.LookupId,
                                ExpectedResourceCount = listItemResource[counter]["ExpectedResourceCount"].ToString(),
                                ProjectId = project.LookupId,
                                ProjectName = project.LookupValue,
                                Skill = skill.LookupValue,
                                SkillId = skill.LookupId
                            };

                            projectSkillResources.Add(projectSkillResource);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllProjectSkillResourcesByProjectID", ex.Message, ex.StackTrace));
            }

            return projectSkillResources;
        }

        public List<OnBoarding> GetBoardingDataFromOnboarding(ref bool sendEmail)
        {
            List<OnBoarding> boardingList = new List<OnBoarding>();
            var showDefaultTraining = "NO";
            var showSkillBasedTraining = "NO";
            var showRoleBasedTraining = "NO";

            try
            {

                var showAssessments = (from config in ConfigItems
                                       where Convert.ToString(config["Title"]) == "ShowUserAssessments"
                                       select Convert.ToString(config["Value1"]).ToUpper()).SingleOrDefault();

                showDefaultTraining = (from config in ConfigItems
                                       where Convert.ToString(config["Title"]) == "ShowDefaultTraining"
                                       select Convert.ToString(config["Value1"]).ToUpper()).SingleOrDefault();

                showSkillBasedTraining = (from config in ConfigItems
                                          where Convert.ToString(config["Title"]) == "ShowSkillBasedTraining"
                                          select Convert.ToString(config["Value1"]).ToUpper()).SingleOrDefault();

                showRoleBasedTraining = (from config in ConfigItems
                                         where Convert.ToString(config["Title"]) == "ShowRoleBasedTraining"
                                         select Convert.ToString(config["Value1"]).ToUpper()).SingleOrDefault();

                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, CurrentUser.SPUserId);
                    ListItemCollection collection = academyOnBoarding.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();

                    if (collection != null && collection.Count > 0)
                    {
                        ListItem item = collection.SingleOrDefault();
                        sendEmail = Convert.ToBoolean(item["SendEmail"]) == true ? true : false;
                        FieldLookupValue location = item["GEO"] as FieldLookupValue;
                        string locVal = location.LookupValue;
                        int locId = location.LookupId;
                        var checkList = GetOnBoardingCheckList(locVal, locId);

                        // Get items from UserCheckList for a user 
                        List userCheckList = context.Web.Lists.GetByTitle(AppConstant.UserCheckList);
                        CamlQuery queryCheckList = new CamlQuery();
                        queryCheckList.ViewXml = @"<View>
                                         <Query>
                                             <Where>
                                                 <Eq>
                                                     <FieldRef Name='User1' LookupId='True' />
                                                     <Value Type='User'>{0}</Value>
                                                 </Eq>
                                             </Where>
                                         </Query>
                                       </View>";
                        queryCheckList.ViewXml = string.Format(queryCheckList.ViewXml, CurrentUser.SPUserId);
                        ListItemCollection userCheckListCollection = userCheckList.GetItems(queryCheckList);
                        context.Load(userCheckListCollection);
                        context.ExecuteQuery();
                        List<UserCheckList> lstUserCheckList = new List<UserCheckList>();
                        if (userCheckListCollection != null && userCheckListCollection.Count() > 0)
                        {
                            foreach (ListItem lstItem in userCheckListCollection)
                            {
                                UserCheckList objUserCheckList = new UserCheckList();
                                objUserCheckList.Id = Convert.ToInt32(lstItem["ID"]);
                                objUserCheckList.Title = Convert.ToString(lstItem["Title"]);
                                objUserCheckList.UserId = item["User1"] != null ? ((FieldLookupValue)(item["User1"])).LookupId : -1;
                                objUserCheckList.UserName = item["User1"] != null ? ((FieldLookupValue)(item["User1"])).LookupValue : "";
                                objUserCheckList.CheckList = Convert.ToString(lstItem["CheckList"]);
                                objUserCheckList.CheckListStatus = Convert.ToString(lstItem["CheckListStatus"]);
                                lstUserCheckList.Add(objUserCheckList);
                            }
                        }
                        for (int i = 0; i < lstUserCheckList.Count; i++)
                        {
                            List<CheckListItem> itemCheckList = checkList.Where(c => c.InternalName.ToUpper() == lstUserCheckList[i].CheckList.ToUpper()).ToList();
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
                            GetUserAssessments(context, boardingList);

                        // Get Default Training from Default Onboarding Assessment
                        Microsoft.SharePoint.Client.ListItemCollection collectionDefault = GetDefaultTrainingAssessment();
                        FieldLookupValue[] objTrainingDefault = new FieldLookupValue[] { };
                        FieldLookupValue[] objAssessment = new FieldLookupValue[] { };

                        if (collectionDefault != null && collectionDefault.Count > 0)
                        {
                            if (collectionDefault.Count > 0)
                            {
                                Microsoft.SharePoint.Client.ListItem itemOnBoard = collectionDefault.SingleOrDefault();
                                objTrainingDefault = itemOnBoard["Training"] as FieldLookupValue[];
                                objAssessment = itemOnBoard["Assessment"] as FieldLookupValue[];
                            }
                        }
                        ArrayList defaultTrainingIds = new ArrayList();
                        if (objTrainingDefault != null)
                        {
                            foreach (FieldLookupValue defaultTrainingItem in objTrainingDefault)
                            {
                                defaultTrainingIds.Add(defaultTrainingItem.LookupId);
                            }
                        }
                        /////Skill Based Trainings//////
                        GetAssignedTrainings(context, CurrentUser, boardingList, defaultTrainingIds, showDefaultTraining, showSkillBasedTraining);

                        /////Role Based Trainings//////
                        if (showRoleBasedTraining == "YES")
                        {
                            List<UserTrainingDetail> userTrainingDetails = new List<UserTrainingDetail>();
                            GetUserRoleBasedTraining(ref userTrainingDetails, CurrentUser.SPUserId);
                            for (int i = 0; i < userTrainingDetails.Count; i++)
                            {
                                OnBoarding boardingItem = new OnBoarding();
                                boardingItem.BoardingItemId = userTrainingDetails[i].Id;
                                boardingItem.BoardingItemName = userTrainingDetails[i].TrainingName;
                                boardingItem.BoardingItemDesc = userTrainingDetails[i].ModuleDesc;
                                boardingItem.BoardingInternalName = userTrainingDetails[i].TrainingName;
                                boardingItem.BoardingIsMandatory = userTrainingDetails[i].Mandatory;
                                if (userTrainingDetails[i].IsLink)
                                    boardingItem.BoardingItemLink = userTrainingDetails[i].LinkUrl;
                                else
                                    boardingItem.BoardingItemLink = userTrainingDetails[i].DocumentUrl;
                                boardingItem.BoardingType = OnboardingItemType.RoleTraining;
                                boardingItem.BoardingStatus = Utilities.GetOnBoardingStatus(userTrainingDetails[i].IsTrainingCompleted, 0, Convert.ToDateTime(userTrainingDetails[i].CompletionDate));
                                boardingList.Add(boardingItem);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetBoardingDataFromOnboarding", ex.Message, ex.StackTrace));
                Utilities.LogToEventVwr(ex.StackTrace.ToString(), 0);
            }
            return boardingList;
        }
        public int GetMarketRiskAssessmentID()
        {
            int assessmentId = 0;
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List academyJoinersCompletion = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <And>
                                                    <Eq>
                                                        <FieldRef Name='User1' LookupId='True' />
                                                        <Value Type='User'>{0}</Value>
                                                    </Eq>
                                                    <And>
                                                        <Eq>
                                                           <FieldRef Name='IsAssessmentActive' />
                                                           <Value Type='Boolean'>1</Value>
                                                        </Eq>
                                                        <Eq>
                                                           <FieldRef Name='Assessment' />
                                                           <Value Type='Lookup'>Market Risk</Value>
                                                        </Eq>
                                                    </And>
                                                </And>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, CurrentUser.SPUserId);

                    ListItemCollection collection = academyJoinersCompletion.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();

                    foreach (ListItem item in collection)
                    {
                        assessmentId = Convert.ToInt32(item.FieldValues["ID"]);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetMarketRiskAssessmentID", ex.Message, ex.StackTrace));

            }

            return assessmentId;
        }

        public List<object> UpdateOnBoardingStatus(List<OnBoardingTrainingStatus> objs)
        {
            List<object> objOnboard = new List<object>();

            try
            {
                foreach (OnBoardingTrainingStatus obj in objs)
                {
                    bool status = false;
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;
                        if (obj.OnboardingType == OnboardingItemType.Training)
                        {
                            using (context)
                            {
                                try
                                {
                                    List academyJoinersTraining = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                                    ListItem objTraining = academyJoinersTraining.GetItemById(obj.Id);
                                    context.Load(objTraining);
                                    context.ExecuteQuery();

                                    objTraining["IsTrainingCompleted"] = true;
                                    objTraining["CompletedDate"] = DateTime.Now;
                                    objTraining.Update();
                                    context.ExecuteQuery();
                                    status = true;
                                    objOnboard.Add(status);
                                    objOnboard.Add(obj.Id);

                                    FieldLookupValue training = objTraining["Training"] as FieldLookupValue;

                                    CamlQuery queryTraining = new CamlQuery();
                                    queryTraining.ViewXml = @"<View><Query>
                                                                <Where>
                                                                    <Eq><FieldRef Name='ID'/>
                                                                        <Value Type='Counter'>{0}</Value>
                                                                    </Eq>                                                                                                    
                                                                </Where>
                                                            </Query>
                                                    </View>";
                                    queryTraining.ViewXml = string.Format(queryTraining.ViewXml, training.LookupId);

                                    List lstTraining = context.Web.Lists.GetByTitle(AppConstant.SkillCompetencyLevelTrainings);
                                    ListItemCollection trainingItems = lstTraining.GetItems(queryTraining);
                                    context.Load(trainingItems);
                                    context.ExecuteQuery();

                                    ListItem trainingItem = trainingItems.SingleOrDefault();
                                    if (trainingItem != null)
                                    {
                                        int points = Convert.ToInt32(trainingItem["Points"]);
                                        FieldLookupValue userSkill = trainingItem["Skill"] as FieldLookupValue;
                                        FieldLookupValue competenceLevel = trainingItem["CompetencyLevel"] as FieldLookupValue;

                                        CamlQuery queryUserPoint = new CamlQuery();
                                        queryUserPoint.ViewXml = @"<View>
                                        <Query>
                                           <Where>
                                             <And>  
                                              <Eq>
                                                  <FieldRef Name='CompetencyLevel' LookupId='True' />
                                                  <Value Type='Lookup'>{0}</Value>
                                              </Eq>
                                              <And>
                                                 <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{1}</Value>
                                                 </Eq>
                                                 <Eq>
                                                    <FieldRef Name='Skill' LookupId='True' />
                                                    <Value Type='Lookup'>{2}</Value>
                                                 </Eq>
                                              </And>
                                             </And>
                                           </Where>
                                        </Query>
                                      </View>";

                                        queryUserPoint.ViewXml = string.Format(queryUserPoint.ViewXml, competenceLevel.LookupId, CurrentUser.SPUserId, userSkill.LookupId);

                                        List lstUserPoints = context.Web.Lists.GetByTitle(AppConstant.UserPoints);
                                        ListItemCollection userPointItems = lstUserPoints.GetItems(queryUserPoint);
                                        context.Load(userPointItems);
                                        context.ExecuteQuery();
                                        if (userPointItems != null && userPointItems.Count > 0)
                                        {
                                            ListItem userPointItem = userPointItems.SingleOrDefault();
                                            if (userPointItem != null)
                                            {
                                                CamlQuery querySkillCompetencyLevel = new CamlQuery();
                                                querySkillCompetencyLevel.ViewXml = @"<View><Query>
                                                                <Where>
                                                                    <Eq><FieldRef Name='ID'/>
                                                                        <Value Type='Counter'>{0}</Value>
                                                                    </Eq>                                                                                                    
                                                                </Where>
                                                            </Query>
                                                    </View>";
                                                querySkillCompetencyLevel.ViewXml = string.Format(querySkillCompetencyLevel.ViewXml, competenceLevel.LookupId);

                                                List lstSkillCompetencyLevel = context.Web.Lists.GetByTitle(AppConstant.SkillCompetencyLevels);
                                                ListItemCollection querySkillCompetencyLevelItems = lstSkillCompetencyLevel.GetItems(querySkillCompetencyLevel);
                                                context.Load(querySkillCompetencyLevelItems);
                                                context.ExecuteQuery();


                                                int currentTrainingPoint = 0;
                                                int currentAssessmentPoint = 0;

                                                if (userPointItem["TrainingPoints"] != null)
                                                {
                                                    string trainingPoints = Convert.ToString(userPointItem["TrainingPoints"]);
                                                    if (trainingPoints.Length > 0)
                                                    {
                                                        int currentPoints = Convert.ToInt32(trainingPoints);
                                                        currentPoints = currentPoints + points;
                                                        userPointItem["TrainingPoints"] = currentPoints;
                                                        currentTrainingPoint = currentPoints;

                                                    }
                                                    else
                                                    {
                                                        userPointItem["TrainingPoints"] = points;
                                                        currentTrainingPoint = points;
                                                    }
                                                }
                                                else
                                                {
                                                    userPointItem["TrainingPoints"] = points;
                                                    currentTrainingPoint = points;
                                                }
                                                if (userPointItem["AssessmentPoints"] != null)
                                                {
                                                    currentAssessmentPoint = Convert.ToInt32(userPointItem["AssessmentPoints"]);
                                                }

                                                bool ifElevate = false;
                                                int competencyLevelOrder = 0;

                                                if (querySkillCompetencyLevelItems != null && querySkillCompetencyLevelItems.Count > 0)
                                                {
                                                    ListItem querySkillCompetencyLevelItem = querySkillCompetencyLevelItems.SingleOrDefault();
                                                    if (querySkillCompetencyLevelItem != null)
                                                    {
                                                        if (querySkillCompetencyLevelItem["TrainingCompletionPoints"] != null)
                                                        {
                                                            int trainingCompletionPoints = 0; int assessmentCompletionPoints = 0;
                                                            if (querySkillCompetencyLevelItem["TrainingCompletionPoints"] != null)
                                                                trainingCompletionPoints = Convert.ToInt32(querySkillCompetencyLevelItem["TrainingCompletionPoints"].ToString());
                                                            if (querySkillCompetencyLevelItem["AssessmentCompletionPoints"] != null)
                                                                assessmentCompletionPoints = Convert.ToInt32(querySkillCompetencyLevelItem["AssessmentCompletionPoints"].ToString());
                                                            if (currentAssessmentPoint >= assessmentCompletionPoints && currentTrainingPoint >= trainingCompletionPoints)
                                                            {
                                                                userPointItem["CompletionStatus"] = true;
                                                                if (querySkillCompetencyLevelItem["CompetencyLevelOrder"] != null)
                                                                {
                                                                    competencyLevelOrder = Convert.ToInt32(querySkillCompetencyLevelItem["CompetencyLevelOrder"].ToString());
                                                                    ifElevate = true;
                                                                }
                                                            }

                                                        }
                                                    }
                                                }

                                                userPointItem.Update();
                                                context.ExecuteQuery();
                                                /////Elevate the user to next competency level/////
                                                if (ifElevate)
                                                {
                                                    CamlQuery querySkillCompetency = new CamlQuery();
                                                    querySkillCompetency.ViewXml = @"<View>
                                        <Query>
                                           <Where>                                                                                          
                                              <And>
                                                 <Eq>
                                                    <FieldRef Name='CompetencyLevelOrder' />
                                                    <Value Type='Number'>{0}</Value>
                                                 </Eq>
                                                 <Eq>
                                                    <FieldRef Name='Skill' LookupId='True' />
                                                    <Value Type='Lookup'>{1}</Value>
                                                 </Eq>
                                              </And>                                             
                                           </Where>
                                        </Query>
                                      </View>";

                                                    querySkillCompetency.ViewXml = string.Format(querySkillCompetency.ViewXml, (competencyLevelOrder + 1), userSkill.LookupId);
                                                    List lstSkillComp = context.Web.Lists.GetByTitle(AppConstant.SkillCompetencyLevels);
                                                    ListItemCollection skillCompItems = lstSkillComp.GetItems(querySkillCompetency);
                                                    context.Load(skillCompItems);
                                                    context.ExecuteQuery();
                                                    if (skillCompItems != null && skillCompItems.Count > 0)
                                                    {
                                                        ListItem skillCompItem = skillCompItems.SingleOrDefault();

                                                        CamlQuery queryUserSkill = new CamlQuery();
                                                        queryUserSkill.ViewXml = @"<View>
                                             <Query>
                                               <Where>                                                                                                   
                                                  <And>
                                                     <Eq>
                                                        <FieldRef Name='User1' LookupId='True' />
                                                        <Value Type='User'>{0}</Value>
                                                     </Eq>
                                                     <Eq>
                                                        <FieldRef Name='Skill' LookupId='True' />
                                                        <Value Type='Lookup'>{1}</Value>
                                                     </Eq>
                                                  </And>                                                 
                                               </Where>
                                            </Query>
                                           </View>";
                                                        queryUserSkill.ViewXml = string.Format(queryUserSkill.ViewXml, CurrentUser.SPUserId, userSkill.LookupId);

                                                        List lstUserSkill = context.Web.Lists.GetByTitle(AppConstant.UserSkills);
                                                        ListItemCollection userSkillItems = lstUserSkill.GetItems(queryUserSkill);
                                                        context.Load(userSkillItems);
                                                        context.ExecuteQuery();
                                                        if (userSkillItems != null && userSkillItems.Count > 0)
                                                        {
                                                            ListItem userSkillItem = userSkillItems.SingleOrDefault();
                                                            userSkillItem["CompetencyLevel"] = skillCompItem.Id;
                                                            userSkillItem.Update();
                                                            context.ExecuteQuery();
                                                            AddSkillBasedTrainingAssessment(skillCompItem.Id.ToString(),
                                                                Convert.ToInt32(userSkill.LookupId), Convert.ToInt32(CurrentUser.SPUserId));

                                                            List listUserPoints = context.Web.Lists.GetByTitle(AppConstant.UserPoints);
                                                            ListItemCreationInformation itemCreateInfoUserPoints = new ListItemCreationInformation();
                                                            ListItem listItemUserPoints = listUserPoints.AddItem(itemCreateInfoUserPoints);
                                                            listItemUserPoints["CompetencyLevel"] = skillCompItem.Id.ToString();
                                                            listItemUserPoints["Skill"] = userSkill.LookupId;
                                                            listItemUserPoints["User1"] = CurrentUser.SPUserId;
                                                            listItemUserPoints["TrainingPoints"] = 0;
                                                            listItemUserPoints["AssessmentPoints"] = 0;
                                                            listItemUserPoints.Update();

                                                            context.ExecuteQuery();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, UpdateOnBoardingStatus", ex.Message, ex.StackTrace));
                                    Utilities.LogToEventVwr(ex.StackTrace.ToString(), 0);
                                }
                            }
                        }
                        else if (obj.OnboardingType == OnboardingItemType.RoleTraining)
                        {
                            using (context)
                            {
                                try
                                {
                                    List academyJoinersTraining = context.Web.Lists.GetByTitle(AppConstant.UserRoleTraining);
                                    ListItem objTraining = academyJoinersTraining.GetItemById(obj.Id);
                                    context.Load(objTraining);
                                    context.ExecuteQuery();
                                    objTraining["IsTrainingCompleted"] = true;
                                    objTraining["CompletedDate"] = DateTime.Now;
                                    objTraining.Update();
                                    context.ExecuteQuery();
                                    status = true;
                                    objOnboard.Add(status);
                                    objOnboard.Add(obj.Id);
                                }
                                catch (Exception ex)
                                {
                                    UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, UpdateOnBoardingStatus", ex.Message, ex.StackTrace));
                                    Utilities.LogToEventVwr(ex.StackTrace.ToString(), 0);
                                }
                            }
                        }
                        else if (obj.OnboardingType == OnboardingItemType.Default)
                        {
                            List checklist = context.Web.Lists.GetByTitle(AppConstant.UserCheckList);
                            ListItem objOnBoarding = checklist.GetItemById(obj.Id);
                            context.Load(objOnBoarding);
                            context.ExecuteQuery();
                            objOnBoarding["CheckListStatus"] = IngEnum.Status.Completed.ToString();
                            objOnBoarding.Update();
                            context.ExecuteQuery();
                            status = true;
                            objOnboard.Add(status);
                            objOnboard.Add(obj.Id);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, UpdateOnBoardingStatus", ex.Message, ex.StackTrace));

            }

            return objOnboard;
        }

        public List<object> UpdateOnBoardingStatus(OnBoardingTrainingStatus obj)
        {
            List<object> objOnboard = new List<object>();
            bool status = false;
            using (ClientContext context = new ClientContext(CurrentSiteUrl))
            {
                context.Credentials = SPCredential;
                if (obj.OnboardingType == OnboardingItemType.Training)////Checklist
                {
                    using (context)
                    {
                        try
                        {
                            List academyJoinersTraining = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                            ListItem objTraining = academyJoinersTraining.GetItemById(obj.Id);
                            context.Load(objTraining);
                            context.ExecuteQuery();
                            objTraining["IsTrainingCompleted"] = true;
                            objTraining["CompletedDate"] = DateTime.Now;
                            objTraining.Update();
                            context.ExecuteQuery();
                            status = true;
                            objOnboard.Add(status);
                            objOnboard.Add(obj.Id);
                            FieldLookupValue training = objTraining["Training"] as FieldLookupValue;
                            CamlQuery queryTraining = new CamlQuery();
                            queryTraining.ViewXml = @"<View><Query>
                                                                <Where>
                                                                    <Eq><FieldRef Name='ID'/>
                                                                        <Value Type='Counter'>{0}</Value>
                                                                    </Eq>                                                                                                    
                                                                </Where>
                                                            </Query>
                                                    </View>";
                            queryTraining.ViewXml = string.Format(queryTraining.ViewXml, training.LookupId);

                            List lstTraining = context.Web.Lists.GetByTitle(AppConstant.SkillCompetencyLevelTrainings);
                            ListItemCollection trainingItems = lstTraining.GetItems(queryTraining);
                            context.Load(trainingItems);
                            context.ExecuteQuery();

                            ListItem trainingItem = trainingItems.SingleOrDefault();
                            if (trainingItem != null)
                            {
                                int points = Convert.ToInt32(trainingItem["Points"]);
                                FieldLookupValue userSkill = trainingItem["Skill"] as FieldLookupValue;
                                FieldLookupValue competenceLevel = trainingItem["CompetencyLevel"] as FieldLookupValue;

                                CamlQuery queryUserPoint = new CamlQuery();
                                queryUserPoint.ViewXml = @"<View>
                                        <Query>
                                           <Where>
                                             <And>  
                                              <Eq>
                                                  <FieldRef Name='CompetencyLevel' LookupId='True' />
                                                  <Value Type='Lookup'>{0}</Value>
                                              </Eq>
                                              <And>
                                                 <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{1}</Value>
                                                 </Eq>
                                                 <Eq>
                                                    <FieldRef Name='Skill' LookupId='True' />
                                                    <Value Type='Lookup'>{2}</Value>
                                                 </Eq>
                                              </And>
                                             </And>
                                           </Where>
                                        </Query>
                                      </View>";

                                queryUserPoint.ViewXml = string.Format(queryUserPoint.ViewXml, competenceLevel.LookupId, CurrentUser.SPUserId, userSkill.LookupId);

                                List lstUserPoints = context.Web.Lists.GetByTitle(AppConstant.UserPoints);
                                ListItemCollection userPointItems = lstUserPoints.GetItems(queryUserPoint);
                                context.Load(userPointItems);
                                context.ExecuteQuery();
                                if (userPointItems != null && userPointItems.Count > 0)
                                {
                                    ListItem userPointItem = userPointItems.SingleOrDefault();
                                    if (userPointItem != null)
                                    {
                                        CamlQuery querySkillCompetencyLevel = new CamlQuery();
                                        querySkillCompetencyLevel.ViewXml = @"<View><Query>
                                                                <Where>
                                                                    <Eq><FieldRef Name='ID'/>
                                                                        <Value Type='Counter'>{0}</Value>
                                                                    </Eq>                                                                                                    
                                                                </Where>
                                                            </Query>
                                                    </View>";
                                        querySkillCompetencyLevel.ViewXml = string.Format(querySkillCompetencyLevel.ViewXml, competenceLevel.LookupId);
                                        List lstSkillCompetencyLevel = context.Web.Lists.GetByTitle(AppConstant.SkillCompetencyLevels);
                                        ListItemCollection querySkillCompetencyLevelItems = lstSkillCompetencyLevel.GetItems(querySkillCompetencyLevel);
                                        context.Load(querySkillCompetencyLevelItems);
                                        context.ExecuteQuery();
                                        int currentTrainingPoint = 0;
                                        int currentAssessmentPoint = 0;

                                        if (userPointItem["TrainingPoints"] != null)
                                        {
                                            string trainingPoints = Convert.ToString(userPointItem["TrainingPoints"]);
                                            if (trainingPoints.Length > 0)
                                            {
                                                int currentPoints = Convert.ToInt32(trainingPoints);
                                                currentPoints = currentPoints + points;
                                                userPointItem["TrainingPoints"] = currentPoints;
                                                currentTrainingPoint = currentPoints;
                                            }
                                            else
                                            {
                                                userPointItem["TrainingPoints"] = points;
                                                currentTrainingPoint = points;
                                            }
                                        }
                                        else
                                        {
                                            userPointItem["TrainingPoints"] = points;
                                            currentTrainingPoint = points;
                                        }
                                        if (userPointItem["AssessmentPoints"] != null)
                                        {
                                            currentAssessmentPoint = Convert.ToInt32(userPointItem["AssessmentPoints"]);
                                        }

                                        bool ifElevate = false;
                                        int competencyLevelOrder = 0;

                                        if (querySkillCompetencyLevelItems != null && querySkillCompetencyLevelItems.Count > 0)
                                        {
                                            ListItem querySkillCompetencyLevelItem = querySkillCompetencyLevelItems.SingleOrDefault();
                                            if (querySkillCompetencyLevelItem != null)
                                            {
                                                if (querySkillCompetencyLevelItem["TrainingCompletionPoints"] != null)
                                                {
                                                    int trainingCompletionPoints = 0; int assessmentCompletionPoints = 0;
                                                    if (querySkillCompetencyLevelItem["TrainingCompletionPoints"] != null)
                                                        trainingCompletionPoints = Convert.ToInt32(querySkillCompetencyLevelItem["TrainingCompletionPoints"].ToString());
                                                    if (querySkillCompetencyLevelItem["AssessmentCompletionPoints"] != null)
                                                        assessmentCompletionPoints = Convert.ToInt32(querySkillCompetencyLevelItem["AssessmentCompletionPoints"].ToString());
                                                    if (currentAssessmentPoint >= assessmentCompletionPoints && currentTrainingPoint >= trainingCompletionPoints)
                                                    {
                                                        userPointItem["CompletionStatus"] = true;
                                                        if (querySkillCompetencyLevelItem["CompetencyLevelOrder"] != null)
                                                        {
                                                            competencyLevelOrder = Convert.ToInt32(querySkillCompetencyLevelItem["CompetencyLevelOrder"].ToString());
                                                            ifElevate = true;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        userPointItem.Update();
                                        context.ExecuteQuery();
                                        /////Elevate the user to next competency level/////
                                        if (ifElevate)
                                        {
                                            CamlQuery querySkillCompetency = new CamlQuery();
                                            querySkillCompetency.ViewXml = @"<View>
                                        <Query>
                                           <Where>                                                                                          
                                              <And>
                                                 <Eq>
                                                    <FieldRef Name='CompetencyLevelOrder' />
                                                    <Value Type='Number'>{0}</Value>
                                                 </Eq>
                                                 <Eq>
                                                    <FieldRef Name='Skill' LookupId='True' />
                                                    <Value Type='Lookup'>{1}</Value>
                                                 </Eq>
                                              </And>                                             
                                           </Where>
                                        </Query>
                                      </View>";

                                            querySkillCompetency.ViewXml = string.Format(querySkillCompetency.ViewXml, (competencyLevelOrder + 1), userSkill.LookupId);
                                            List lstSkillComp = context.Web.Lists.GetByTitle(AppConstant.SkillCompetencyLevels);
                                            ListItemCollection skillCompItems = lstSkillComp.GetItems(querySkillCompetency);
                                            context.Load(skillCompItems);
                                            context.ExecuteQuery();
                                            if (skillCompItems != null && skillCompItems.Count > 0)
                                            {
                                                ListItem skillCompItem = skillCompItems.SingleOrDefault();
                                                CamlQuery queryUserSkill = new CamlQuery();
                                                queryUserSkill.ViewXml = @"<View>
                                             <Query>
                                               <Where>                                                                                                   
                                                  <And>
                                                     <Eq>
                                                        <FieldRef Name='User1' LookupId='True' />
                                                        <Value Type='User'>{0}</Value>
                                                     </Eq>
                                                     <Eq>
                                                        <FieldRef Name='Skill' LookupId='True' />
                                                        <Value Type='Lookup'>{1}</Value>
                                                     </Eq>
                                                  </And>                                                 
                                               </Where>
                                            </Query>
                                           </View>";
                                                queryUserSkill.ViewXml = string.Format(queryUserSkill.ViewXml, CurrentUser.SPUserId, userSkill.LookupId);

                                                List lstUserSkill = context.Web.Lists.GetByTitle(AppConstant.UserSkills);
                                                ListItemCollection userSkillItems = lstUserSkill.GetItems(queryUserSkill);
                                                context.Load(userSkillItems);
                                                context.ExecuteQuery();
                                                if (userSkillItems != null && userSkillItems.Count > 0)
                                                {
                                                    ListItem userSkillItem = userSkillItems.SingleOrDefault();
                                                    userSkillItem["CompetencyLevel"] = skillCompItem.Id;
                                                    userSkillItem.Update();
                                                    context.ExecuteQuery();
                                                    AddSkillBasedTrainingAssessment(skillCompItem.Id.ToString(), Convert.ToInt32(userSkill.LookupId), Convert.ToInt32(CurrentUser.SPUserId));
                                                    List listUserPoints = context.Web.Lists.GetByTitle(AppConstant.UserPoints);
                                                    ListItemCreationInformation itemCreateInfoUserPoints = new ListItemCreationInformation();
                                                    ListItem listItemUserPoints = listUserPoints.AddItem(itemCreateInfoUserPoints);
                                                    listItemUserPoints["CompetencyLevel"] = skillCompItem.Id.ToString();
                                                    listItemUserPoints["Skill"] = userSkill.LookupId;
                                                    listItemUserPoints["User1"] = CurrentUser.SPUserId;
                                                    listItemUserPoints["TrainingPoints"] = 0;
                                                    listItemUserPoints["AssessmentPoints"] = 0;
                                                    listItemUserPoints.Update();
                                                    context.ExecuteQuery();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                            LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, UpdateOnBoardingStatus", ex.Message, ex.StackTrace));
                            Utilities.LogToEventVwr(ex.StackTrace.ToString(), 0);
                        }
                    }
                }
                else if (obj.OnboardingType == OnboardingItemType.Default)////Checklist
                {
                    List checklist = context.Web.Lists.GetByTitle(AppConstant.UserCheckList);
                    ListItem objOnBoarding = checklist.GetItemById(obj.Id);
                    context.Load(objOnBoarding);
                    context.ExecuteQuery();
                    objOnBoarding["CheckListStatus"] = IngEnum.Status.Completed.ToString();
                    //objOnBoarding["SendEmail"] = false;
                    objOnBoarding.Update();
                    context.ExecuteQuery();
                    status = true;
                    objOnboard.Add(status);
                    objOnboard.Add(obj.Id);
                }
            }
            return objOnboard;
        }
        public bool EmilOnBoardingStatus()
        {
            bool status = false;

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                    context.Load(context.Web);
                    context.Load(academyOnBoarding);
                    context.ExecuteQuery();
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, CurrentUser.SPUserId);
                    ListItemCollection collection = academyOnBoarding.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();

                    if (collection != null && collection.Count > 0)
                    {
                        foreach (ListItem item in collection)
                        {
                            string SendMail = item["SendEmail"] != null ? Convert.ToString(item["SendEmail"]) : "";

                            if (SendMail != "Yes")
                            {
                                int onboardID = Convert.ToInt32(item["ID"]);
                                string ModifiedDt = Convert.ToString(item["Modified"]);
                                ProfileSharingEmail(context, onboardID, ModifiedDt);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, EmilOnBoardingStatus", ex.Message, ex.StackTrace));

            }
            return status;
        }

        public void GetOnBoardingProfile()
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    string query = @"<View><Query><Where><Eq><FieldRef Name='User1' LookupId='True' /><Value Type='User'>" + CurrentUser.SPUserId + "</Value></Eq></Where></Query></View>";
                    ListItemCollection onboardingItems = SharePointUtil.GetListItems(AppConstant.AcademyOnBoarding, context, null, query);
                    if (onboardingItems != null && onboardingItems.Count > 0)
                    {
                        CurrentUser.OnBoardingID = onboardingItems[0].Id;
                        AttachmentCollection files = onboardingItems[0].AttachmentFiles;
                        context.Load(files);
                        context.ExecuteQuery();
                        if (files != null & files.Count > 0)
                        {
                            Attachment file = files.LastOrDefault();
                            context.Load(file, fg => fg.ServerRelativeUrl, fg => fg.FileName);
                            context.ExecuteQuery();
                            CurrentUser.HasAttachments = true;
                            var serverUrl = new Uri(context.Url).GetLeftPart(UriPartial.Authority);

                            CurrentUser.FileName = serverUrl + file.ServerRelativeUrl;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetOnBoardingProfile", ex.Message, ex.StackTrace));

            }
        }

        public List<OnboardingHelp> GetOnboardingHelp()
        {
            List<OnboardingHelp> onboardHelp = new List<OnboardingHelp>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    ListItemCollection lstItemsOnboardHelp = SharePointUtil.GetListItems(AppConstant.OnboardingHelp, context, null, string.Empty);

                    if (lstItemsOnboardHelp != null & lstItemsOnboardHelp.Count > 0)
                    {
                        foreach (ListItem item in lstItemsOnboardHelp)
                        {
                            OnboardingHelp onboardItem = new OnboardingHelp();
                            onboardItem.Title = Convert.ToString(item["Title"]);
                            onboardItem.Description = StripHTML(Convert.ToString(item["Description1"]));
                            onboardItem.OrderingId = Convert.ToInt32(item["OrderingId"]);
                            onboardHelp.Add(onboardItem);
                        }
                    }

                    var orderByResult = from i in onboardHelp
                                        orderby i.OrderingId
                                        select i;

                    onboardHelp = new List<OnboardingHelp>();
                    foreach (var item in orderByResult)
                        onboardHelp.Add(item);
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetOnboardingHelp", ex.Message, ex.StackTrace));

            }

            return onboardHelp;
        }

        public void UploadProfile(string fileName, byte[] fileByteArray)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    string spFileRelativeURL = string.Empty;
                    AddAttachmentToOnBoardingList(
                        fileByteArray,
                        context,
                        fileName,
                        out spFileRelativeURL);

                    var serverUrl = new Uri(context.Url).GetLeftPart(UriPartial.Authority);
                    CurrentUser.FileName = serverUrl + spFileRelativeURL;
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, UploadProfile", ex.Message, ex.StackTrace));

            }
        }
        public List<RoleTraining> GetRoleTrainings(int roleId)
        {
            List<RoleTraining> roleTrainings = HttpContext.Current.Session[AppConstant.AllRoleTrainingData] as List<RoleTraining>;
            if (roleTrainings == null)
            {
                roleTrainings = new List<RoleTraining>();
                try
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;
                        ListItemCollection lstTraining = SharePointUtil.GetListItems(AppConstant.RoleTraining, context, null, null);
                        if (lstTraining != null & lstTraining.Count > 0)
                        {
                            RoleTraining trainingItem = null;
                            foreach (ListItem item in lstTraining)
                            {
                                string _URL = string.Empty;

                                if (item.FieldValues["_x0055_RL1"] != null)
                                {
                                    if (string.IsNullOrEmpty(item.FieldValues["_x0055_RL1"].ToString()) == false)
                                    {
                                        var hyperLink = ((FieldUrlValue)(item.FieldValues["_x0055_RL1"]));
                                        _URL = hyperLink != null ? hyperLink.Url : string.Empty; ;


                                    }
                                }

                                FieldLookupValue[] roles = ((FieldLookupValue[])item["Roles"]);
                                if (roles.Length > 1)
                                {
                                    for (int j = 0; j < roles.Length; j++)
                                    {
                                        trainingItem = new RoleTraining()
                                        {
                                            TrainingId = item.Id,
                                            TrainingName = Convert.ToString(item["Title"]),
                                            IsMandatory = Convert.ToBoolean(item["IsMandatory"]),
                                            RoleId = roles[j].LookupId,
                                            URL = _URL
                                        };
                                        roleTrainings.Add(trainingItem);
                                    }
                                }
                                else if (roles.Length == 1)
                                {
                                    trainingItem = new RoleTraining()
                                    {
                                        TrainingId = item.Id,
                                        TrainingName = Convert.ToString(item["Title"]),
                                        IsMandatory = Convert.ToBoolean(item["IsMandatory"]),
                                        RoleId = roles[0].LookupId,
                                        URL = _URL
                                    };
                                    roleTrainings.Add(trainingItem);
                                }
                            }
                            HttpContext.Current.Session[AppConstant.AllRoleTrainingData] = roleTrainings;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetTrainings", ex.Message, ex.StackTrace));

                }
            }

            List<RoleTraining> filterTrainings = roleTrainings.Where(t => t.RoleId == roleId).ToList();
            return filterTrainings;

        }
        public List<Training> GetTrainings(int skillId, int competenceId)
        {
            List<Training> trainings = HttpContext.Current.Session[AppConstant.AllTrainingData] as List<Training>;
            if (trainings == null)
            {
                trainings = new List<Training>();
                try
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;
                        ListItemCollection lstTraining = SharePointUtil.GetListItems(AppConstant.SkillCompetencyLevelTrainings, context, null, null);
                        if (lstTraining != null & lstTraining.Count > 0)
                        {
                            Training trainingItem = null;
                            foreach (ListItem item in lstTraining)
                            {
                                trainingItem = new Training()
                                {
                                    TrainingId = item.Id,
                                    TrainingName = Convert.ToString(item["Title"]),
                                    IsMandatory = Convert.ToBoolean(item["IsMandatory"]),
                                    SkillId = ((FieldLookupValue)item["Skill"]).LookupId,
                                    CompetencyId = ((FieldLookupValue)item["CompetencyLevel"]).LookupId
                                };
                                trainings.Add(trainingItem);
                            }
                            HttpContext.Current.Session[AppConstant.AllTrainingData] = trainings;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetTrainings", ex.Message, ex.StackTrace));

                }
            }
            if (competenceId == 0)
            {
                List<Training> filterTrainings = trainings.Where(t => t.SkillId == skillId).ToList();
                return filterTrainings;
            }
            else
            {
                List<Training> filterTrainings = trainings.Where(t => t.SkillId == skillId && t.CompetencyId == competenceId).ToList();
                return filterTrainings;
            }
        }
        public ListItemCollection GetDefaultTrainingAssessment()
        {
            ListItemCollection collection = HttpContext.Current.Session[AppConstant.DefaultTrainingAssessmentData] as ListItemCollection;
            if (collection != null)
                return collection;
            else
            {
                try
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;
                        collection = SharePointUtil.GetListItems(AppConstant.DefaultOnboardingTrainingAssessment, context, null, null);
                        HttpContext.Current.Session[AppConstant.DefaultTrainingAssessmentData] = collection;

                    }
                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetDefaultTrainingAssessment", ex.Message, ex.StackTrace));

                }
                return collection;
            }

        }
        public void AddRoleBasedTraining(int roleId, int userId)
        {
            using (ClientContext context = new ClientContext(CurrentSiteUrl))
            {
                context.Credentials = SPCredential;
                List userRoleTrainingList = context.Web.Lists.GetByTitle(AppConstant.UserRoleTraining);
                List<RoleTraining> trainings = GetRoleTrainings(roleId);
                ListItemCreationInformation listitemCreateInfo = new ListItemCreationInformation();
                int lastDayCompletion = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LastDayCompletion"]);
                foreach (RoleTraining item in trainings)
                {
                    ListItem TraininglistItems = userRoleTrainingList.AddItem(listitemCreateInfo);
                    TraininglistItems["Title"] = "training";
                    TraininglistItems["IsMandatory"] = item.IsMandatory == true ? 1 : 0;
                    //   TraininglistItems["IsTrainingActive"] = true;
                    TraininglistItems["IsTrainingCompleted"] = false;
                    TraininglistItems["LastDayCompletion"] = DateTime.Now.AddDays(lastDayCompletion);
                    List<FieldLookupValue> roleLookupValueList = new List<FieldLookupValue>();
                    roleLookupValueList.Add(new FieldLookupValue { LookupId = (Convert.ToInt32(roleId)) });
                    TraininglistItems["Roles"] = roleLookupValueList;
                    TraininglistItems["RoleBasedTraining"] = item.TrainingId;
                    TraininglistItems["User1"] = userId;
                    TraininglistItems.Update();
                }
                context.ExecuteQuery();
            }

        }
        public void AddRoleBasedTraining(int roleId, int userId, bool isMandatory, DateTime lastDayCompletion)
        {
            using (ClientContext context = new ClientContext(CurrentSiteUrl))
            {
                context.Credentials = SPCredential;
                List userRoleTrainingList = context.Web.Lists.GetByTitle(AppConstant.UserRoleTraining);
                List<RoleTraining> trainings = GetRoleTrainings(roleId);
                ListItemCreationInformation listitemCreateInfo = new ListItemCreationInformation();
                foreach (RoleTraining item in trainings)
                {
                    ListItem TraininglistItems = userRoleTrainingList.AddItem(listitemCreateInfo);
                    TraininglistItems["Title"] = "training";
                    TraininglistItems["IsMandatory"] = isMandatory;
                    //   TraininglistItems["IsTrainingActive"] = true;
                    TraininglistItems["IsTrainingCompleted"] = false;
                    TraininglistItems["LastDayCompletion"] = lastDayCompletion;
                    List<FieldLookupValue> roleLookupValueList = new List<FieldLookupValue>();
                    roleLookupValueList.Add(new FieldLookupValue { LookupId = (Convert.ToInt32(roleId)) });
                    TraininglistItems["Roles"] = roleLookupValueList;
                    TraininglistItems["RoleBasedTraining"] = item.TrainingId;
                    TraininglistItems["User1"] = userId;
                    TraininglistItems.Update();
                }
                context.ExecuteQuery();
            }
        }
        public void AddSkillBasedTrainingAssessment(string competence, int skillId, int userId)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    ArrayList defaultAssessmentIds = new ArrayList();
                    ArrayList defaultTrainingIds = new ArrayList();

                    BuildDefaultTrainingAssessmentArray(defaultTrainingIds, defaultAssessmentIds);

                    #region Add Non-DefaultTraining and Assessment based on skill and competancy

                    context.Load(context.Web);
                    context.ExecuteQuery();
                    List<Training> SkillBasedTrainings = new List<Training>();
                    List<Assessment> SkillBasedAssessments = new List<Assessment>();
                    SkillBasedTrainings = GetTrainings(skillId, Convert.ToInt32(competence));
                    SkillBasedAssessments = GetAssessments(skillId, Convert.ToInt32(competence));

                    List Traininglist = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                    List Assessmentlist = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
                    ListItemCreationInformation listitemCreateInfo = new ListItemCreationInformation();
                    int lastDayCompletion = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LastDayCompletion"]);
                    foreach (Training item in SkillBasedTrainings)
                    {
                        if (!defaultTrainingIds.Contains(item.TrainingId))
                        {
                            ListItem TraininglistItems = Traininglist.AddItem(listitemCreateInfo);
                            TraininglistItems["Title"] = "training";
                            TraininglistItems["IsIncludeOnBoarding"] = true;
                            TraininglistItems["IsMandatory"] = item.IsMandatory == true ? 1 : 0;
                            TraininglistItems["IsTrainingActive"] = true;
                            TraininglistItems["IsTrainingCompleted"] = false;
                            TraininglistItems["LastDayCompletion"] = DateTime.Now.AddDays(lastDayCompletion);
                            TraininglistItems["Skill"] = skillId;
                            TraininglistItems["Training"] = item.TrainingId;
                            TraininglistItems["User1"] = userId;
                            TraininglistItems["NewTrainingAdditionMailer"] = context.Web.Url.ToString() + ", Mail Sent";
                            TraininglistItems.Update();
                        }
                    }
                    //   context.ExecuteQuery();              

                    foreach (Assessment Aitem in SkillBasedAssessments)
                    {
                        if (!defaultAssessmentIds.Contains(Aitem.AssessmentId))
                        {
                            ListItem AssessmentlistItems = Assessmentlist.AddItem(listitemCreateInfo);
                            AssessmentlistItems["Training"] = Aitem.TrainingId;
                            AssessmentlistItems["User1"] = userId;
                            AssessmentlistItems["Title"] = "assessment";
                            AssessmentlistItems["IsIncludeOnBoarding"] = true;
                            AssessmentlistItems["IsMandatory"] = Aitem.IsMandatory == true ? 1 : 0;
                            AssessmentlistItems["IsAssessmentActive"] = 1;
                            AssessmentlistItems["IsAssessmentComplete"] = 0;
                            AssessmentlistItems["LastDayCompletion"] = DateTime.Now.AddDays(lastDayCompletion);
                            AssessmentlistItems["Skill"] = skillId; ;
                            AssessmentlistItems["Assessment"] = Aitem.AssessmentId;
                            AssessmentlistItems.Update();
                        }
                    }
                    context.ExecuteQuery();
                    #endregion
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AddSkillBasedTrainingAssessment", ex.Message, ex.StackTrace));

            }
        }

        public List<Assessment> GetAssessments(int skillId, int competenceId)
        {
            List<Assessment> assessments = HttpContext.Current.Session[AppConstant.AllAssessmentData] as List<Assessment>;
            if (assessments == null)
            {
                assessments = new List<Assessment>();
                try
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;
                        ListItemCollection lstAssessment = SharePointUtil.GetListItems(AppConstant.Assessments, context, null, null);
                        if (lstAssessment != null & lstAssessment.Count > 0)
                        {
                            Assessment assessment = null;
                            foreach (ListItem item in lstAssessment)
                            {
                                assessment = new Assessment()
                                {
                                    AssessmentId = item.Id,
                                    AssessmentName = Convert.ToString(item["Title"]),
                                    IsMandatory = Convert.ToBoolean(item["IsMandatory"]),
                                    SkillId = ((FieldLookupValue)item["Skill"]).LookupId,
                                    CompetencyId = ((FieldLookupValue)item["CompetencyLevel"]).LookupId
                                };
                                assessments.Add(assessment);
                            }
                            HttpContext.Current.Session[AppConstant.AllAssessmentData] = assessments;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAssessments", ex.Message, ex.StackTrace));

                }
            }
            if (competenceId == 0)
            {
                List<Assessment> filterAssessments = assessments.Where(t => t.SkillId == skillId).ToList();
                return filterAssessments;
            }
            else
            {
                List<Assessment> filterAssessments = assessments.Where(t => t.SkillId == skillId && t.CompetencyId == competenceId).ToList();
                return filterAssessments;
            }
        }

        private List<Assessment> GetAllAssessments()
        {
            List<Assessment> assessments = new List<Assessment>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    string query = "";
                    ListItemCollection lstAssessment = SharePointUtil.GetListItems(AppConstant.Assessments, context, null, query);

                    if (lstAssessment != null & lstAssessment.Count > 0)
                    {
                        Assessment assessment = null;
                        foreach (ListItem item in lstAssessment)
                        {
                            string _AssessmentLink = null;
                            if (item.FieldValues["AssessmentLink"] != null)
                            {
                                _AssessmentLink = ((FieldUrlValue)item.FieldValues["AssessmentLink"]).Url;
                            }

                            int _AssessmentTimeInMins = 0;
                            if (item.FieldValues["AssessmentTimeInMins"] != null)
                            {
                                _AssessmentTimeInMins = Convert.ToInt32(item.FieldValues["AssessmentTimeInMins"].ToString());
                            }

                            assessment = new Assessment();

                            assessment.AssessmentId = Convert.ToInt32(item["ID"].ToString());
                            assessment.AssessmentName = Convert.ToString(item["Title"]);
                            if (item["IsMandatory"] != null)
                                assessment.IsMandatory = Convert.ToBoolean(item["IsMandatory"]);
                            assessment.AssessmentLink = _AssessmentLink;
                            assessment.AssessmentTimeInMins = _AssessmentTimeInMins;
                            if (item["Description1"] != null)
                                assessment.Description = Convert.ToString(item["Description1"]);
                            assessments.Add(assessment);
                        }
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

        public void AddSkillBasedTrainingAssessment(string competence, int skillId, int userId, bool isTrainingMandatory, DateTime lastDayOfCompletion)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    ArrayList defaultTrainingIds = new ArrayList();
                    ArrayList defaultAssessmentIds = new ArrayList();

                    BuildDefaultTrainingAssessmentArray(defaultTrainingIds, defaultAssessmentIds);

                    #region Add Non-DefaultTraining and Assessment based on skill and competancy

                    context.Load(context.Web);
                    context.ExecuteQuery();
                    List<Training> SkillBasedTrainings = new List<Training>();
                    List<Assessment> SkillBasedAssessments = new List<Assessment>();

                    SkillBasedTrainings = GetTrainings(skillId, Convert.ToInt32(competence));
                    SkillBasedAssessments = GetAssessments(skillId, Convert.ToInt32(competence));

                    //Adding non-default trainings to user taraining mapping list
                    List Traininglist = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                    ListItemCreationInformation TrainingListitemCreateInfo = new ListItemCreationInformation();

                    List Assessmentlist = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
                    ListItemCreationInformation AssessmentitemCreateInfo = new ListItemCreationInformation();

                    foreach (Training item in SkillBasedTrainings)
                    {
                        if (!defaultTrainingIds.Contains(item.TrainingId))
                        {
                            Microsoft.SharePoint.Client.ListItem TraininglistItems = Traininglist.AddItem(TrainingListitemCreateInfo);
                            DateTime currentTimeKind = DateTime.SpecifyKind(lastDayOfCompletion, DateTimeKind.Local);
                            TraininglistItems["Title"] = "training";
                            TraininglistItems["IsIncludeOnBoarding"] = true;
                            TraininglistItems["IsMandatory"] = isTrainingMandatory == true ? 1 : 0;
                            TraininglistItems["IsTrainingActive"] = true;
                            TraininglistItems["IsTrainingCompleted"] = false;
                            TraininglistItems["LastDayCompletion"] = currentTimeKind;
                            TraininglistItems["Skill"] = skillId;
                            TraininglistItems["Training"] = item.TrainingId;
                            TraininglistItems["User1"] = userId;
                            TraininglistItems["NewTrainingAdditionMailer"] = context.Web.Url.ToString() + ", Mail Sent";
                            TraininglistItems.Update();
                        }
                    }
                    //  context.ExecuteQuery();

                    foreach (Assessment Aitem in SkillBasedAssessments)
                    {
                        if (!defaultAssessmentIds.Contains(Aitem.AssessmentId))
                        {
                            ListItem AssessmentlistItems = Assessmentlist.AddItem(AssessmentitemCreateInfo);
                            DateTime currentTimeKind = DateTime.SpecifyKind(lastDayOfCompletion, DateTimeKind.Local);
                            // Adding Tranings to AcademyJoinersCompletion
                            AssessmentlistItems["Training"] = Aitem.TrainingId;
                            AssessmentlistItems["User1"] = userId;
                            AssessmentlistItems["Title"] = "assessment";
                            AssessmentlistItems["IsIncludeOnBoarding"] = true;
                            AssessmentlistItems["IsMandatory"] = isTrainingMandatory == true ? 1 : 0;
                            AssessmentlistItems["IsAssessmentActive"] = 1;
                            AssessmentlistItems["IsAssessmentComplete"] = 0;
                            AssessmentlistItems["LastDayCompletion"] = currentTimeKind;
                            AssessmentlistItems["Skill"] = skillId; ;
                            AssessmentlistItems["Assessment"] = Aitem.AssessmentId;
                            AssessmentlistItems.Update();
                        }
                    }
                    context.ExecuteQuery();

                    #endregion
                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AddSkillBasedTrainingAssessment", ex.Message, ex.StackTrace));

            }
        }

        public List<UserTraining> GetUserTrainingsByTrainingID(int TrainingId)
        {
            List<UserTraining> userTrainings = new List<UserTraining>();

            using (ClientContext context = new ClientContext(CurrentSiteUrl))
            {
                context.Credentials = SPCredential;

                string query = @"<View>
                                        <Query>
                                            <Where>                                               
                                                <Eq>
                                                    <FieldRef Name='Training' LookupId='True' />
                                                    <Value Type='Lookup'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                query = string.Format(query, TrainingId);

                ListItemCollection lstUserTrainings = SharePointUtil.GetListItems(AppConstant.UserTrainingMapping, context, null, query);

                if (lstUserTrainings != null & lstUserTrainings.Count > 0)
                {
                    foreach (ListItem item in lstUserTrainings)
                    {
                        bool _IsTrainingCompleted = false;
                        string employee = ((FieldLookupValue)(item.FieldValues["User1"])).LookupValue;

                        if (item["IsTrainingCompleted"] != null)
                        {
                            _IsTrainingCompleted = Convert.ToBoolean(item["IsTrainingCompleted"]);
                        }
                        UserTraining userTraining = new UserTraining()
                        {
                            Employee = employee,
                            IsTrainingCompleted = _IsTrainingCompleted
                        };
                        userTrainings.Add(userTraining);
                    }
                }
            }
            return userTrainings;
        }
        private void BuildDefaultTrainingAssessmentArray(ArrayList defaultTrainingIds, ArrayList defaultAssessmentIds)
        {
            ListItemCollection collection = GetDefaultTrainingAssessment();
            if (collection != null && collection.Count > 0)
            {
                ListItem itemOnBoard = collection.SingleOrDefault();
                FieldLookupValue[] objTraining = itemOnBoard["Training"] as FieldLookupValue[];
                FieldLookupValue[] objAssessment = itemOnBoard["Assessment"] as FieldLookupValue[];

                foreach (FieldLookupValue item in objTraining)
                {
                    defaultTrainingIds.Add(item.LookupId);
                }
                if (objAssessment != null)
                {
                    foreach (FieldLookupValue item in objAssessment)
                    {
                        defaultAssessmentIds.Add(item.LookupId);
                    }
                }
            }
        }
        private void RemoveSkillBasedTrainingAssessment(ClientContext context, int competence, int skillId, int userId)
        {
            List<Training> skillBasedTrainings = new List<Training>();
            List<Assessment> skillBasedAssessments = new List<Assessment>();
            ArrayList defaultTrainingIds = new ArrayList();
            ArrayList defaultAssessmentIds = new ArrayList();

            BuildDefaultTrainingAssessmentArray(defaultTrainingIds, defaultAssessmentIds);

            ////Remove all trainings and assessments for the skill and competency except the default ones////////
            skillBasedTrainings = GetTrainings(skillId, competence);
            skillBasedAssessments = GetAssessments(skillId, competence);

            ListItemCollection userTrainingCollection = null;
            List userTrainingList = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
            CamlQuery userTrainingQuery = new CamlQuery();

            userTrainingQuery.ViewXml = @"<View>
						<Query>
							<Where>                                           
								<And>
									<Eq>
										<FieldRef Name='User1' LookupId='True' />
										<Value Type='User'>{0}</Value>
									</Eq>									
									<Eq>
										   <FieldRef Name='Skill' LookupId='True' />
										   <Value Type='Lookup'>{1}</Value>
									</Eq>									
								</And>                                            
							</Where>
						</Query>
				</View>";
            userTrainingQuery.ViewXml = string.Format(userTrainingQuery.ViewXml, userId, skillId);
            userTrainingCollection = userTrainingList.GetItems(userTrainingQuery);

            ListItemCollection userAssessmentCollection = null;
            List userAssessmentList = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
            CamlQuery userAssessmentQuery = new CamlQuery();

            userAssessmentQuery.ViewXml = @"<View>
						<Query>
							<Where>                                           
								<And>
									<Eq>
										<FieldRef Name='User1' LookupId='True' />
										<Value Type='User'>{0}</Value>
									</Eq>								
									<Eq>
                                        <FieldRef Name='Skill' LookupId='True' />
										<Value Type='Lookup'>{1}</Value>
									</Eq>
								</And>                                            
							</Where>
						</Query>
				</View>";
            userAssessmentQuery.ViewXml = string.Format(userAssessmentQuery.ViewXml, userId, skillId);
            userAssessmentCollection = userAssessmentList.GetItems(userAssessmentQuery);
            context.Load(userAssessmentCollection);
            context.Load(userTrainingCollection);
            context.ExecuteQuery();

            List<UserTraining> userTrainings = new List<UserTraining>();
            if (userTrainingCollection != null && userTrainingCollection.Count > 0)
            {
                foreach (ListItem item in userTrainingCollection)
                {
                    UserTraining u = new UserTraining();
                    u.Id = item.Id;
                    u.SkillId = ((FieldLookupValue)item["Training"]).LookupId;
                    u.TrainingId = ((FieldLookupValue)item["Skill"]).LookupId;
                    userTrainings.Add(u);
                }
            }
            List<UserAssessment> userAssessments = new List<UserAssessment>();
            if (userAssessmentCollection != null && userAssessmentCollection.Count > 0)
            {
                foreach (ListItem item in userAssessmentCollection)
                {
                    UserAssessment u = new UserAssessment();
                    u.TrainingAssessmentId = ((FieldLookupValue)item["Assessment"]).LookupId;
                    u.TrainingId = ((FieldLookupValue)item["Skill"]).LookupId;
                    u.Id = item.Id;
                    userAssessments.Add(u);
                }
            }

            foreach (Training item in skillBasedTrainings)
            {
                if (!defaultTrainingIds.Contains(item.TrainingId))
                {
                    List<UserTraining> filteredTrainings = userTrainings.Where(t => t.TrainingId == item.TrainingId).ToList();
                    if (filteredTrainings != null & filteredTrainings.Count > 0)
                    {
                        foreach (UserTraining i in filteredTrainings)
                        {
                            userTrainingList.GetItemById(i.Id).DeleteObject();
                        }
                    }
                }
            }

            foreach (Assessment item in skillBasedAssessments)
            {
                if (!defaultAssessmentIds.Contains(item.AssessmentId))
                {
                    List<UserAssessment> filteredAssessments = userAssessments.Where(t => t.TrainingAssessmentId == item.AssessmentId).ToList();
                    if (filteredAssessments != null & filteredAssessments.Count > 0)
                    {
                        foreach (UserAssessment i in filteredAssessments)
                        {
                            userAssessmentList.GetItemById(i.Id).DeleteObject();
                        }
                    }
                }
            }
            context.ExecuteQuery();

            if (skillBasedAssessments != null && skillBasedAssessments.Count > 0)
            {
                List listAssessmentsHistory = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentHistory);
                CamlQuery queryAssessment = new CamlQuery();
                queryAssessment.ViewXml = @"<View>
                                <Query>
                                    <Where>  
                                            <And> 
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='Lookup'>{0}</Value>
                                                </Eq>
                                                <Eq>
										            <FieldRef Name='Assessment' LookupId='True'/>
										            <Value Type='Lookup'>{1}</Value>
										        </Eq>
                                            </And>
                                    </Where>
                                </Query>
                                </View>";
                queryAssessment.ViewXml = string.Format(queryAssessment.ViewXml, userId, skillBasedAssessments[0].AssessmentId);
                ListItemCollection listItemAssessmentHistory = listAssessmentsHistory.GetItems(queryAssessment);
                context.Load(listItemAssessmentHistory);
                context.ExecuteQuery();

                if (listItemAssessmentHistory != null && listItemAssessmentHistory.Count > 0)
                {
                    foreach (Microsoft.SharePoint.Client.ListItem item in listItemAssessmentHistory)
                    {
                        listAssessmentsHistory.GetItemById(item.Id).DeleteObject();
                    }
                    context.ExecuteQuery();
                }
            }
        }
        private void RemoveRoleBasedTraining(ClientContext context, int roleId, int userId)
        {
            List<Training> skillBasedTrainings = new List<Training>();
            ////Remove all trainings and assessments for the skill and competency except the default ones////////
            ListItemCollection userTrainingCollection = null;
            List userTrainingList = context.Web.Lists.GetByTitle(AppConstant.UserRoleTraining);
            CamlQuery userTrainingQuery = new CamlQuery();
            userTrainingQuery.ViewXml = @"<View>
						<Query>
							<Where>                                           
								<And>
									<Eq>
										<FieldRef Name='User1' LookupId='True' />
										<Value Type='User'>{0}</Value>
									</Eq>									
									<Eq>
										   <FieldRef Name='Roles' LookupId='True' />
										   <Value Type='Lookup'>{1}</Value>
									</Eq>									
								</And>                                            
							</Where>
						</Query>
				</View>";
            userTrainingQuery.ViewXml = string.Format(userTrainingQuery.ViewXml, userId, roleId);
            userTrainingCollection = userTrainingList.GetItems(userTrainingQuery);
            context.Load(userTrainingCollection);
            context.ExecuteQuery();

            foreach (ListItem item in userTrainingCollection)
            {
                userTrainingList.GetItemById(item.Id).DeleteObject();
            }
            context.ExecuteQuery();
        }
        public List<UserAssessment> GetUserAssessmentsByAssessmentId(int AssessmentId)
        {
            List<UserAssessment> userAssessments = new List<UserAssessment>();

            using (ClientContext context = new ClientContext(CurrentSiteUrl))
            {
                context.Credentials = SPCredential;

                string query = @"<View>
                                        <Query>
                                            <Where>                                               
                                                <Eq>
                                                    <FieldRef Name='Assessment' LookupId='True' />
                                                    <Value Type='Lookup'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                query = string.Format(query, AssessmentId);

                ListItemCollection lstUserAssessment = SharePointUtil.GetListItems(AppConstant.UserAssessmentMapping, context, null, query);

                if (lstUserAssessment != null & lstUserAssessment.Count > 0)
                {
                    foreach (ListItem item in lstUserAssessment)
                    {
                        bool _IsAssessmentComplete = false;
                        string employee = "";

                        if (item["IsAssessmentComplete"] != null)
                        {
                            _IsAssessmentComplete = Convert.ToBoolean(item["IsAssessmentComplete"]);
                            employee = ((FieldLookupValue)(item.FieldValues["User1"])).LookupValue;
                            UserAssessment userAssessment = new UserAssessment()
                            {
                                IsAssessmentComplete = _IsAssessmentComplete,
                                Employee = employee
                            };
                            userAssessments.Add(userAssessment);
                        }
                    }
                }
            }

            return userAssessments;
        }

        public List<UserAssessment> GetUserAssessmentsByID(int ID)
        {
            List<UserAssessment> userAssessments = new List<UserAssessment>();

            using (ClientContext context = new ClientContext(CurrentSiteUrl))
            {
                context.Credentials = SPCredential;

                List userAssessmentMapping = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
                CamlQuery query = new CamlQuery();
                query.ViewXml = @"<View>
                                <Query>
                                    <Where>
                                            <Eq>
                                                <FieldRef Name='ID' />
                                                <Value Type='Counter'>{0}</Value>
                                            </Eq>
                                    </Where>
                                </Query>
                                </View>";
                query.ViewXml = string.Format(query.ViewXml, ID);
                ListItemCollection collection = userAssessmentMapping.GetItems(query);
                context.Load(collection);
                context.ExecuteQuery();

                foreach (ListItem item in collection)
                {

                    UserAssessment userAssessment = new UserAssessment()
                    {
                        CompletedDate = Convert.ToString(item["CompletedDate"]),
                        TrainingAssessment = (item["Assessment"] as FieldLookupValue).LookupValue,
                        MarksInPercentage = Convert.ToDecimal(item["MarksInPercentage"])
                    };

                    userAssessments.Add(userAssessment);
                }

            }
            return userAssessments;
        }

        public List<GEO> GetAllGEOs()
        {
            List<GEO> geos = HttpContext.Current.Session[AppConstant.AllGEOData] as List<GEO>;

            if (geos == null)
            {
                geos = new List<GEO>();

                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    ListItemCollection lstGEO = SharePointUtil.GetListItems(AppConstant.AcademyGEO, context, null, null);

                    if (lstGEO != null & lstGEO.Count > 0)
                    {
                        GEO geo = null;
                        foreach (ListItem item in lstGEO)
                        {
                            geo = new GEO()
                            {
                                Id = item.Id,
                                Title = Convert.ToString(item["Title"])
                            };
                            geos.Add(geo);
                        }
                        HttpContext.Current.Session[AppConstant.AllGEOData] = geos;
                    }
                }
            }
            return geos;
        }
        public List<Role> GetAllRoles()
        {
            List<Role> roles = HttpContext.Current.Session[AppConstant.AllRoleData] as List<Role>;

            if (roles == null)
            {
                roles = new List<Role>();
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    ListItemCollection lstRoles = SharePointUtil.GetListItems(AppConstant.Roles, context, null, null);

                    if (lstRoles != null & lstRoles.Count > 0)
                    {
                        foreach (ListItem item in lstRoles)
                        {
                            Role r = new Role();
                            r.Id = item.Id;
                            r.Title = Convert.ToString(item["Title"]);
                            roles.Add(r);
                        }
                        HttpContext.Current.Session[AppConstant.AllRoleData] = roles;
                    }
                }
            }
            return roles;
        }
        public List<UserRole> GetRoleForOnboardedUser(int userId)
        {
            List<UserRole> lstUserRoles = new List<UserRole>();
            using (ClientContext context = new ClientContext(CurrentSiteUrl))
            {
                context.Credentials = SPCredential;
                List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                CamlQuery query = new CamlQuery();
                query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                query.ViewXml = string.Format(query.ViewXml, userId);
                ListItemCollection collection = academyOnBoarding.GetItems(query);
                context.Load(collection);
                context.ExecuteQuery();

                if (collection != null && collection.Count > 0)
                {
                    ListItem item = collection.SingleOrDefault();
                    FieldLookupValue[] roles = ((FieldLookupValue[])item["Roles"]);
                    UserRole userRoleItem = new UserRole();
                    if (roles.Length > 1)
                    {
                        for (int j = 0; j < roles.Length; j++)
                        {
                            userRoleItem = new UserRole()
                            {
                                RoleId = roles[j].LookupId,
                                RoleName = roles[j].LookupValue
                                //UserId = userId
                            };
                            lstUserRoles.Add(userRoleItem);
                        }
                    }
                    else if (roles.Length == 1)
                    {
                        userRoleItem = new UserRole()
                        {
                            RoleId = roles[0].LookupId,
                            RoleName = roles[0].LookupValue
                            //UserId = userId
                        };
                        lstUserRoles.Add(userRoleItem);
                    }
                }
            }
            return lstUserRoles;
        }
        public UserManager GetUserByEmail(string searchEmail)
        {
            UserManager user = null;
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    if (!string.IsNullOrEmpty(searchEmail))
                    {
                        ClientResult<Microsoft.SharePoint.Client.Utilities.PrincipalInfo> persons =
                        Microsoft.SharePoint.Client.Utilities.Utility.ResolvePrincipal(
                            context,
                            context.Web,
                            searchEmail,
                            Microsoft.SharePoint.Client.Utilities.PrincipalType.User,
                            Microsoft.SharePoint.Client.Utilities.PrincipalSource.All,
                            null,
                            true
                        );
                        context.ExecuteQuery();
                        Microsoft.SharePoint.Client.Utilities.PrincipalInfo person = persons.Value;
                        User userFound = context.Web.EnsureUser(person.LoginName);

                        context.Load(userFound);
                        context.ExecuteQuery();

                        user = new UserManager();
                        user.Designation = person.JobTitle;
                        user.UserName = userFound.Title;
                        user.EmailID = userFound.Email;
                        user.SPUserId = userFound.Id;
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager loggeduser = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, loggeduser.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserByEmail", ex.Message, ex.StackTrace));

            }
            return user;
        }

        public bool AddUserToGroup(string email)
        {
            bool result = false;

            UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
            LogHelper.AddLog(new LogEntity(AppConstant.PartitionInformation, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AddUserToGroup", "In AddUserToGroup", "User id:" + email));

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    string spAcademyMemberGroup = ConfigurationManager.AppSettings["AcademyMemberGroup"].ToString();
                    User oUser = null;
                    Microsoft.SharePoint.Client.Group spGrp = null;
                    spGrp = context.Web.SiteGroups.GetByName(spAcademyMemberGroup);
                    context.Load(spGrp);


                    if (IsOnline)
                    {
                        oUser = context.Web.EnsureUser("i:0#.f|membership|" + email);

                    }
                    else
                    {
                        oUser = context.Web.EnsureUser(email);
                    }
                    context.Load(oUser);

                    // Adding users to the Group                      
                    spGrp.Users.AddUser(oUser);
                    spGrp.Update();
                    context.ExecuteQuery();

                    result = true;
                }
            }
            catch (Exception ex)
            {

                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AddUserToGroup", ex.Message, ex.StackTrace));


            }
            return result;
        }

        public bool OnboardEmail(string email, int UserId, string UserName)
        {
            bool result = false;
            try
            {

                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    context.Load(context.Web, w => w.Url);
                    context.ExecuteQuery();

                    Hashtable hashtable = new Hashtable();
                    Hashtable Training_HT = new Hashtable();

                    #region Logic for sending assessment mail while onboarding
                    string strAssessmentname = string.Empty;

                    List<UserAssessment> AssessmentLi = GetAssessmentForUser(UserId);
                    foreach (UserAssessment itemAsses in AssessmentLi)
                    {
                        DateTime dt = Convert.ToDateTime(itemAsses.LastDayCompletion);
                        strAssessmentname = strAssessmentname + itemAsses.TrainingAssessment + " Last Completion Date " + dt.ToString("MMMM dd") + ", ";
                    }

                    string strTrainingName = string.Empty;

                    List<UserTraining> trainingLi = GetTrainingForUser(UserId, false);
                    foreach (UserTraining itemTraining in trainingLi)
                    {
                        DateTime dt = Convert.ToDateTime(itemTraining.LastDayCompletion);
                        strTrainingName = strTrainingName + itemTraining.TrainingName + " Last Completion Date " + dt.ToString("MMMM dd") + ", ";
                    }

                    hashtable.Add("UserName", UserName);
                    hashtable.Add("ClientName", ConfigurationManager.AppSettings["ClientName"].ToString());
                    hashtable.Add("WebUrl", context.Web.Url.ToString());
                    hashtable.Add("Assessment", strAssessmentname);
                    hashtable.Add("Training", strTrainingName);
                    bool Queue1 = SharePointUtil.AddToEmailQueue(context, "EmployeeOnboardMail", hashtable, email, null);

                    #endregion
                    //GetAssessmentForUser

                    #region Logic for sending training mail while onboarding
                    List<UserTraining> traningLi = GetTrainingForUser(UserId);

                    string trainingTable = string.Empty;
                    trainingTable += "<table border='1' cellspacing='0' cellpadding='0' style='border-collapse: collapse; border: none;'>";
                    trainingTable += "<tbody>";
                    trainingTable += "<tr>";
                    trainingTable += "<td><b>Training Name</b></td>";
                    //   trainingTable += "<td><b>Training Course</b></td>";
                    trainingTable += "<td><b>Last Date of Completion</b></td>";
                    trainingTable += "<td><b>Mandatory?</b></td>";
                    trainingTable += "</tr>";

                    foreach (UserTraining item in traningLi)
                    {
                        trainingTable += "<tr>";
                        trainingTable += "<td>" + item.TrainingName + "</td>";
                        //     trainingTable += "<td>" + item.TrainingCourse + "</td>";
                        trainingTable += "<td>" + item.LastDayCompletion + "</td>";
                        trainingTable += "<td>" + item.IsMandatory + "</td>";
                        trainingTable += "</tr>";
                    }

                    List<UserTrainingDetail> trainings = new List<UserTrainingDetail>();
                    GetUserRoleBasedTraining(ref trainings, UserId);
                    for (int i = 0; i < trainings.Count; i++)
                    {
                        trainingTable += "<tr>";
                        trainingTable += "<td>" + trainings[i].TrainingName + "</td>";
                        //     trainingTable += "<td>" + item.TrainingCourse + "</td>";
                        trainingTable += "<td>" + trainings[i].CompletionDate + "</td>";
                        trainingTable += "<td>" + trainings[i].Mandatory + "</td>";
                        trainingTable += "</tr>";
                    }


                    trainingTable += "</tbody>";
                    trainingTable += "</table>";

                    Hashtable data = new Hashtable();
                    data.Add("TrainingTable", trainingTable);
                    data.Add("UserName", UserName);
                    data.Add("ClientName", ConfigurationManager.AppSettings["ClientName"].ToString());
                    data.Add("WebUrl", context.Web.Url.ToString());
                    data.Add("Assessment", strAssessmentname);

                    bool Queue2 = SharePointUtil.AddToEmailQueue(context, "EmployeeOnboardTrainingMail", data, email, null);

                    #endregion

                    result = true;

                }

            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, OnboardEmail", ex.Message, ex.StackTrace));

            }

            return result;
        }

        public void OnBoardUser(string competence, int skillId, int userId, string geo, int roleId, string userEmail, string userName)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    if (context != null)
                    {
                        Microsoft.SharePoint.Client.Web w = context.Web;
                        context.Load(w);
                        context.ExecuteQuery();

                        ////////// Adding item to AcademyOnBoarding ///////////////
                        List list = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                        ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                        ListItem listItem = list.AddItem(itemCreateInfo);
                        listItem["CompetencyLevel"] = competence;
                        listItem["Skill"] = skillId;
                        listItem["User1"] = userId;
                        listItem["GEO"] = geo;
                        listItem["EmployeeOnboardedMail"] = w.Url.ToString() + ", Mail Sent";
                        List<FieldLookupValue> roleLookupValueList = new List<FieldLookupValue>();
                        roleLookupValueList.Add(new FieldLookupValue { LookupId = (Convert.ToInt32(roleId)) });
                        listItem["Roles"] = roleLookupValueList;
                        listItem.Update();
                        ///////////////////////////////////////////////////////////////////////////////////////

                        ////////////////Adding item to userpoints/////////////
                        List listUserPoints = context.Web.Lists.GetByTitle(AppConstant.UserPoints);
                        ListItemCreationInformation itemCreateInfoUserPoints = new ListItemCreationInformation();
                        ListItem listItemUserPoints = listUserPoints.AddItem(itemCreateInfoUserPoints);
                        listItemUserPoints["CompetencyLevel"] = competence;
                        listItemUserPoints["Skill"] = skillId;
                        listItemUserPoints["User1"] = userId;
                        listItemUserPoints["TrainingPoints"] = 0;
                        listItemUserPoints["AssessmentPoints"] = 0;
                        listItemUserPoints.Update();
                        context.ExecuteQuery();

                        /////////////////////////////////////////////////////////

                        ////Add skill in UserSkill list//////////////
                        List userSkilllist = context.Web.Lists.GetByTitle(AppConstant.UserSkills);
                        ListItemCreationInformation skillitemCreateInfo = new ListItemCreationInformation();
                        ListItem skilllistItem = userSkilllist.AddItem(skillitemCreateInfo);
                        skilllistItem["CompetencyLevel"] = competence;
                        skilllistItem["Skill"] = skillId;
                        skilllistItem["User1"] = userId;
                        skilllistItem.Update();
                        context.ExecuteQuery();
                        //////////////////////////////////////////////////////////

                        ///////// Assign Default Trainings //////////////////////////

                        ////////Get list of default trainings and assessments//
                        FieldLookupValue[] defaultTrainings = new FieldLookupValue[] { };
                        FieldLookupValue[] defaultAssessments = new FieldLookupValue[] { };
                        ListItemCollection collection = GetDefaultTrainingAssessment();

                        if (collection != null && collection.Count > 0)
                        {
                            ListItem itemOnBoard = collection.SingleOrDefault();
                            defaultTrainings = itemOnBoard["Training"] as FieldLookupValue[];
                            defaultAssessments = itemOnBoard["Assessment"] as FieldLookupValue[];
                        }

                        List<UserTraining> trainings = new List<UserTraining>();
                        UserTraining objTraining = null;
                        ArrayList defaultTrainingIds = new ArrayList();
                        int lastDayCompletion = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LastDayCompletion"]);
                        if (defaultTrainings != null)
                        {
                            foreach (FieldLookupValue item in defaultTrainings)
                            {
                                int skill = GetSkillForTraining(item.LookupId);

                                objTraining = new UserTraining()
                                {
                                    IsIncludeOnBoarding = true,
                                    IsMandatory = true,
                                    LastDayCompletion = Convert.ToString(DateTime.Now.AddDays(lastDayCompletion)),
                                    SkillId = skill,
                                    TrainingId = item.LookupId
                                };
                                trainings.Add(objTraining);
                                defaultTrainingIds.Add(item.LookupId);
                            }
                        }
                        AssignTrainingsToUser(trainings, userId, true);

                        ///////// Assign default assignments//////////
                        List<UserAssessment> assessments = new List<UserAssessment>();
                        UserAssessment objAssessment = null;
                        ArrayList defaultAssessmentIds = new ArrayList();

                        if (defaultAssessments != null)
                        {
                            foreach (FieldLookupValue item in defaultAssessments)
                            {
                                int skill = GetSkillForTraining(GetTrainingForAssessment(item.LookupId));
                                objAssessment = new UserAssessment()
                                {
                                    IsIncludeOnBoarding = true,
                                    IsMandatory = true,
                                    LastDayCompletion = Convert.ToString(DateTime.Now.AddDays(lastDayCompletion)),
                                    SkillId = skill,
                                    TrainingAssessment = item.LookupValue,
                                    TrainingAssessmentId = item.LookupId
                                };
                                assessments.Add(objAssessment);
                                defaultAssessmentIds.Add(item.LookupId);
                            }
                            AssignAssessmentsToUser(assessments, userId, true);
                        }
                        /////////////////////////////////////////////////////////////////

                        ////////Assign skill based trainings and assessments/////////////

                        AddSkillBasedTrainingAssessment(competence, skillId, userId);

                        ////////////////////////////////////////////////////////////////////

                        //////////Assign Role based trainings//////////////////

                        AddRoleBasedTraining(roleId, userId);

                        ////////////////////////////////////////////////////////

                        /////////// Assign Checklist items/////////////
                        List<CheckListItem> checkListItems = GetAllChecklist();
                        List userCheckList = context.Web.Lists.GetByTitle(AppConstant.UserCheckList);

                        //////////// Insert default checklist items into UserCheckList//////////////
                        List<CheckListItem> defaultChecklistItems = checkListItems.Where(c => c.GEOId == Convert.ToInt32(geo) && c.RoleName.ToUpper() == "DEFAULT").ToList();
                        foreach (var item in defaultChecklistItems)
                        {
                            ListItemCreationInformation userCheckListCreateInfo = new ListItemCreationInformation();
                            Microsoft.SharePoint.Client.ListItem userCheckListListItem = userCheckList.AddItem(userCheckListCreateInfo);
                            userCheckListListItem["Title"] = "CheckList";
                            userCheckListListItem["User1"] = userId;
                            userCheckListListItem["CheckList"] = item.InternalName;
                            userCheckListListItem["CheckListStatus"] = "";
                            userCheckListListItem.Update();
                        }
                        /////////////If selected role is not default then assign role wise checklist items///////////////
                        List<Role> roles = GetAllRoles();
                        Role selectedRole = roles.Where(r => r.Id == roleId).FirstOrDefault();
                        if (selectedRole.Title.ToUpper() != "DEFAULT")
                        {
                            List<CheckListItem> rolewiseChecklistItems = checkListItems.Where(c => c.GEOId == Convert.ToInt32(geo) && c.RoleId == roleId).ToList();
                            foreach (var item in rolewiseChecklistItems)
                            {
                                ListItemCreationInformation userCheckListCreateInfo = new ListItemCreationInformation();
                                Microsoft.SharePoint.Client.ListItem userCheckListListItem = userCheckList.AddItem(userCheckListCreateInfo);
                                userCheckListListItem["Title"] = "CheckList";
                                userCheckListListItem["User1"] = userId;
                                userCheckListListItem["CheckList"] = item.InternalName;
                                userCheckListListItem["CheckListStatus"] = "";
                                userCheckListListItem.Update();
                            }
                        }
                        context.ExecuteQuery();

                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, OnBoardUser", ex.Message, ex.StackTrace));
                Utilities.LogToEventVwr(ex.StackTrace, 0);
            }
        }
        public List<CheckListItem> GetAllChecklist()
        {

            List<CheckListItem> items = HttpContext.Current.Session[AppConstant.AllCheckListData] as List<CheckListItem>;
            if (items == null)
            {
                items = new List<CheckListItem>();
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    ListItemCollection checkList = SharePointUtil.GetListItems(AppConstant.OnBoardingCheckList, context, null, null);

                    if (checkList != null && checkList.Count > 0)
                    {
                        for (int i = 0; i < checkList.Count; i++)
                        {
                            FieldLookupValue[] roles = ((FieldLookupValue[])checkList[i]["Roles"]);
                            if (roles.Length > 1)
                            {
                                for (int j = 0; j < roles.Length; j++)
                                {
                                    CheckListItem item = new CheckListItem();
                                    item.Name = checkList[i]["Title"].ToString();
                                    item.InternalName = checkList[i]["InternalName"].ToString();
                                    FieldLookupValue geo = ((FieldLookupValue)checkList[i]["GEO"]);
                                    item.GEOId = geo.LookupId;
                                    item.GEOName = geo.LookupValue;
                                    if (checkList[i]["Description1"] != null)
                                        item.Desc = checkList[i]["Description1"].ToString();
                                    if (checkList[i]["TypeChoice"] != null)
                                        item.Choice = checkList[i]["TypeChoice"].ToString();
                                    item.RoleId = roles[j].LookupId;
                                    item.RoleName = roles[j].LookupValue;
                                    items.Add(item);
                                }
                            }
                            else if (roles.Length == 1)
                            {
                                CheckListItem item = new CheckListItem();
                                item.Name = checkList[i]["Title"].ToString();
                                item.InternalName = checkList[i]["InternalName"].ToString();
                                FieldLookupValue geo = ((FieldLookupValue)checkList[i]["GEO"]);
                                item.GEOId = geo.LookupId;
                                item.GEOName = geo.LookupValue;
                                if (checkList[i]["Description1"] != null)
                                    item.Desc = checkList[i]["Description1"].ToString();
                                if (checkList[i]["TypeChoice"] != null)
                                    item.Choice = checkList[i]["TypeChoice"].ToString();
                                item.RoleId = roles[0].LookupId;
                                item.RoleName = roles[0].LookupValue;
                                items.Add(item);
                            }
                        }
                        HttpContext.Current.Session[AppConstant.AllCheckListData] = items;
                    }

                }
            }

            return items;
        }
        public UserOnBoarding GetOnBoardingDetailsForUser(UserManager user)
        {
            UserOnBoarding objUserOnBoarding = new UserOnBoarding();
            objUserOnBoarding.UserId = user.SPUserId;
            objUserOnBoarding.Name = user.UserName;
            objUserOnBoarding.Email = user.EmailID;

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, user.SPUserId);
                    ListItemCollection collection = academyOnBoarding.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();
                    if (collection != null && collection.Count > 0)
                    {
                        ListItem item = collection.FirstOrDefault();

                        // Adding Skills and Competence
                        objUserOnBoarding.CurrentSkill = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "";
                        objUserOnBoarding.CurrentCompetance = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupValue : "";// item["CompetencyLevel"] != null ? Convert.ToString(item["CompetencyLevel"]) : "";
                        objUserOnBoarding.CurrentStatus = item["Status"] != null ? Convert.ToString(item["Status"]) : "";
                        objUserOnBoarding.CurrentBGVStatus = item["BGVStatus"] != null ? Convert.ToString(item["BGVStatus"]) : "";
                        objUserOnBoarding.CurrentProfileSharing = item["ProfileSharing"] != null ? Convert.ToString(item["ProfileSharing"]) : "";
                        objUserOnBoarding.CurrentGEO = item["GEO"] != null ? ((FieldLookupValue)(item["GEO"])).LookupValue : "";
                        objUserOnBoarding.IsPresentInOnBoard = true;
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager loggeduser = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, loggeduser.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetOnBoardingDetailsForUser", ex.Message, ex.StackTrace));

            }

            return objUserOnBoarding;
        }

        public List<UserAssessment> GetAssessmentForUser(int userId, bool OnlyOnBoardedTraining = false)
        {
            List<UserAssessment> lstUserAssessments = new List<UserAssessment>();
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List academyAssessment = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
                    CamlQuery query = new CamlQuery();
                    if (OnlyOnBoardedTraining)
                    {
                        query.ViewXml = @"<View>
                                    <Query>
                                        <Where>                                           
                                            <And>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                                    <Eq>
                                                         <FieldRef Name='IsIncludeOnBoarding' />
                                                         <Value Type='Boolean'>{1}</Value>
                                                    </Eq>                                                    
                                                </And>                                            
                                        </Where>
                                    </Query>
                            </View>";
                        query.ViewXml = string.Format(query.ViewXml, userId, 1);
                    }
                    else
                    {
                        query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                        query.ViewXml = string.Format(query.ViewXml, userId);
                    }
                    ListItemCollection collection = academyAssessment.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();

                    UserAssessment objUserAssessment = null;
                    if (collection != null && collection.Count > 0)
                    {
                        foreach (var item in collection)
                        {
                            objUserAssessment = new UserAssessment();

                            var localTime = context.Web.RegionalSettings.TimeZone.UTCToLocalTime(DateTime.Parse(item["LastDayCompletion"].ToString()));
                            context.ExecuteQuery();
                            DateTime eventDate = localTime.Value;

                            objUserAssessment.CompletedDate = Convert.ToString(item["CompletedDate"]);
                            objUserAssessment.IsIncludeOnBoarding = Convert.ToBoolean(item["IsIncludeOnBoarding"]);
                            objUserAssessment.IsMandatory = Convert.ToBoolean(item["IsMandatory"]);
                            objUserAssessment.IsAssessmentActive = Convert.ToBoolean(item["IsAssessmentActive"]);
                            objUserAssessment.IsAssessmentComplete = Convert.ToBoolean(item["IsAssessmentComplete"]);
                            objUserAssessment.LastDayCompletion = Convert.ToDateTime(item["LastDayCompletion"].ToString()).ToShortDateString();
                            objUserAssessment.SkillName = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "";
                            objUserAssessment.SkillId = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupId : 0;
                            objUserAssessment.TrainingName = item["Training"] != null ? ((FieldLookupValue)(item["Training"])).LookupValue : "";
                            objUserAssessment.TrainingId = item["Training"] != null ? ((FieldLookupValue)(item["Training"])).LookupId : 0;
                            objUserAssessment.TrainingAssessment = item["Assessment"] != null ? ((FieldLookupValue)(item["Assessment"])).LookupValue : "";
                            objUserAssessment.TrainingAssessmentId = item["Assessment"] != null ? ((FieldLookupValue)(item["Assessment"])).LookupId : 0;

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
                            lstUserAssessments.Add(objUserAssessment);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAssessmentForUser", ex.Message, ex.StackTrace));

            }

            return lstUserAssessments;
        }

        public bool AssignAssessmentsToUser(List<UserAssessment> assessments, int userId, bool forDefault = false)
        {
            bool result = false;
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    foreach (UserAssessment item in assessments)
                    {
                        List list = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
                        ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                        ListItem listItem = list.AddItem(itemCreateInfo);

                        // Adding Tranings to AcademyJoinersCompletion
                        listItem["Training"] = GetTrainingForAssessment(item.TrainingAssessmentId);
                        listItem["User1"] = userId;

                        listItem["Title"] = "assessment";
                        listItem["IsIncludeOnBoarding"] = item.IsIncludeOnBoarding == true ? 1 : 0;
                        listItem["IsMandatory"] = item.IsMandatory == true ? 1 : 0;
                        listItem["IsAssessmentActive"] = 1;
                        listItem["IsAssessmentComplete"] = 0;
                        listItem["LastDayCompletion"] = Convert.ToDateTime(item.LastDayCompletion);
                        listItem["Skill"] = item.SkillId;//item.TrainingCourseId;
                        listItem["Assessment"] = item.TrainingAssessmentId;

                        listItem.Update();

                        context.ExecuteQuery();

                        // Adding to onboarding List
                        if (item.IsIncludeOnBoarding && forDefault == false)
                        {
                            //   string onBoardingColumn = "TrainingAssessment";
                            //UpadateOnBoardingData(context, userId, onBoardingColumn, item.TrainingAssessmentId);
                        }

                    }

                }
                result = true;
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AssignAssessmentsToUser", ex.Message, ex.StackTrace));
                Utilities.LogToEventVwr(ex.Message, 0);
            }
            return result;
        }

        public List<UserManager> GetAllOnBoardedUser(int assignedTo)
        {
            List<UserManager> lstUserManager = new List<UserManager>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    string query = null;

                    if (assignedTo > 0)
                    {
                        query = @"<View>
                            <Query>
                                <Where>
                                    <Eq>
                                        <FieldRef Name='Skill' LookupId='True' />
                                        <Value Type='Lookup'>{0}</Value>
                                    </Eq>
                                </Where>
                            </Query>
                        </View>";
                        query = string.Format(query, assignedTo);

                    }

                    ListItemCollection lstOnBoarding = SharePointUtil.GetListItems(AppConstant.AcademyOnBoarding, context, null, query);

                    if (lstOnBoarding != null & lstOnBoarding.Count > 0)
                    {
                        UserManager objUser = null;
                        foreach (ListItem item in lstOnBoarding)
                        {
                            objUser = new UserManager()
                            {
                                EmailID = ((FieldUserValue)(item.FieldValues["User1"])).Email,
                                SPUserId = ((FieldLookupValue)(item.FieldValues["User1"])).LookupId,
                                UserName = ((FieldLookupValue)(item.FieldValues["User1"])).LookupValue

                            };
                            lstUserManager.Add(objUser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllOnBoardedUser", ex.Message, ex.StackTrace));

            }

            return lstUserManager;

        }

        public bool AssignTrainingsToUser(List<UserTraining> trainings, int userId, bool forDefault = false)
        {
            bool result = false;
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    context.Load(context.Web);
                    context.ExecuteQuery();

                    foreach (UserTraining item in trainings)
                    {
                        List list = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                        ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                        ListItem listItem = list.AddItem(itemCreateInfo);

                        // Adding Tranings to AcademyJoinersTraining
                        listItem["Title"] = "training";
                        listItem["IsIncludeOnBoarding"] = item.IsIncludeOnBoarding;
                        listItem["IsMandatory"] = item.IsMandatory;
                        listItem["IsTrainingActive"] = true;
                        listItem["IsTrainingCompleted"] = false;
                        listItem["LastDayCompletion"] = Convert.ToDateTime(item.LastDayCompletion);
                        listItem["Skill"] = item.SkillId;
                        listItem["Training"] = item.TrainingId;
                        listItem["User1"] = userId;
                        listItem["NewTrainingAdditionMailer"] = context.Web.Url.ToString() + ", Mail Sent"; // Workflow column being updated 

                        listItem.Update();

                        context.ExecuteQuery();
                        if (item.IsIncludeOnBoarding && forDefault == false)
                        {
                            //   string onBoardingColumn = "Training";
                            //  UpadateOnBoardingData(context, userId, onBoardingColumn, item.TrainingModuleId);
                        }
                    }
                }

                result = true;
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AssignTrainingsToUser", ex.Message, ex.StackTrace));
                Utilities.LogToEventVwr(ex.Message, 0);
            }
            return result;
        }
        public bool AddSkill(string email, string userId, string skillId, string competence, bool ismandatory, DateTime lastdayofcompletion)
        {
            bool returnVal = false;

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List list = context.Web.Lists.GetByTitle(AppConstant.UserSkills);
                    ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                    ListItem listItem = list.AddItem(itemCreateInfo);

                    if (context != null)
                    {
                        listItem["CompetencyLevel"] = competence;
                        listItem["Skill"] = skillId;
                        listItem["User1"] = userId;
                        listItem.Update();

                        List listUserPoints = context.Web.Lists.GetByTitle(AppConstant.UserPoints);
                        ListItemCreationInformation itemCreateInfoUserPoints = new ListItemCreationInformation();
                        ListItem listItemUserPoints = listUserPoints.AddItem(itemCreateInfoUserPoints);
                        listItemUserPoints["CompetencyLevel"] = competence;
                        listItemUserPoints["Skill"] = skillId;
                        listItemUserPoints["User1"] = userId;
                        listItemUserPoints["TrainingPoints"] = 0;
                        listItemUserPoints["AssessmentPoints"] = 0;
                        listItemUserPoints.Update();

                        context.ExecuteQuery();

                        AddSkillBasedTrainingAssessment(
                            competence,
                            Convert.ToInt32(skillId),
                            Convert.ToInt32(userId),
                            ismandatory,
                            lastdayofcompletion
                        );
                        returnVal = true;
                    }
                    else
                    {
                        returnVal = false;
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AddSkill", ex.Message, ex.StackTrace));

            }

            return returnVal;
        }

        public bool AddRole(string email, string userId, string roleId, bool ismandatory, DateTime lastdayofcompletion)
        {
            bool returnVal = false;
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, userId);
                    ListItemCollection collection = academyOnBoarding.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();

                    if (collection != null && collection.Count > 0)
                    {
                        ListItem item = collection.SingleOrDefault();
                        FieldLookupValue[] roles = ((FieldLookupValue[])item["Roles"]);
                        List<FieldLookupValue> roleLookupValueList = new List<FieldLookupValue>();
                        for (int j = 0; j < roles.Length; j++)
                        {
                            roleLookupValueList.Add(new FieldLookupValue { LookupId = roles[j].LookupId });
                        }
                        roleLookupValueList.Add(new FieldLookupValue { LookupId = Convert.ToInt32(roleId) });
                        item["Roles"] = roleLookupValueList;
                        item.Update();
                        context.ExecuteQuery();
                        AddRoleBasedTraining(Convert.ToInt32(roleId), Convert.ToInt32(userId), ismandatory, lastdayofcompletion);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AddSkill", ex.Message, ex.StackTrace));
            }
            return returnVal;
        }
        public List<UserSkill> GetSkillForUser(int userId)
        {
            List<UserSkill> lstSkills = new List<UserSkill>();
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List userSkills = context.Web.Lists.GetByTitle(AppConstant.UserSkills);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, userId);


                    ListItemCollection collection = userSkills.GetItems(query);

                    context.Load(collection);
                    context.ExecuteQuery();

                    UserSkill objSkill = null;
                    if (collection != null && collection.Count > 0)
                    {
                        foreach (var item in collection)
                        {
                            objSkill = new UserSkill();
                            objSkill.Id = item.Id;
                            objSkill.Skill = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "";
                            objSkill.SkillId = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupId : -1;
                            objSkill.Competence = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupValue : ""; //Convert.ToString(item["CompetencyLevel"]);
                            objSkill.CompetenceId = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupId : -1;
                            List<SkillCompetencies> competencies = GetCompetenciesBySkill(objSkill.Skill);
                            objSkill.SkillwiseCompetencies = "";
                            objSkill.SkillwiseCompetencyIds = "";
                            for (int i = 0; i < competencies.Count; i++)
                            {
                                if (objSkill.SkillwiseCompetencies.Length > 0)
                                    objSkill.SkillwiseCompetencies = objSkill.SkillwiseCompetencies + "|" + competencies[i].CompetenceName;
                                else
                                    objSkill.SkillwiseCompetencies = competencies[i].CompetenceName;

                                if (objSkill.SkillwiseCompetencyIds.Length > 0)
                                    objSkill.SkillwiseCompetencyIds = objSkill.SkillwiseCompetencyIds + "|" + competencies[i].CompetenceId.ToString();
                                else
                                    objSkill.SkillwiseCompetencyIds = competencies[i].CompetenceId.ToString();
                            }
                            lstSkills.Add(objSkill);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetSkillForUser", ex.Message, ex.StackTrace));

            }

            return lstSkills;
        }

        public List<UserSkill> GetSkillForOnboardedUser(int userId)
        {
            List<UserSkill> lstSkills = new List<UserSkill>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List userSkills = context.Web.Lists.GetByTitle(AppConstant.UserSkills);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, userId);


                    ListItemCollection collection = userSkills.GetItems(query);

                    context.Load(collection);
                    context.ExecuteQuery();

                    UserSkill objSkill = null;
                    List<Competence> allCompetencies = GetAllCompetenceList();
                    if (collection != null && collection.Count > 0)
                    {
                        foreach (var item in collection)
                        {
                            objSkill = new UserSkill();
                            objSkill.Id = item.Id;
                            objSkill.Skill = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "";
                            objSkill.SkillId = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupId : -1;
                            objSkill.Competence = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupValue : "";
                            objSkill.CompetenceId = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupId : -1;
                            List<SkillCompetencies> competencies = GetCompetenciesBySkillForOnboardedUser(objSkill.SkillId, allCompetencies);
                            objSkill.SkillwiseCompetencies = "";
                            objSkill.SkillwiseCompetencyIds = "";
                            for (int i = 0; i < competencies.Count; i++)
                            {
                                if (objSkill.SkillwiseCompetencies.Length > 0)
                                    objSkill.SkillwiseCompetencies = objSkill.SkillwiseCompetencies + "|" + competencies[i].CompetenceName;
                                else
                                    objSkill.SkillwiseCompetencies = competencies[i].CompetenceName;

                                if (objSkill.SkillwiseCompetencyIds.Length > 0)
                                    objSkill.SkillwiseCompetencyIds = objSkill.SkillwiseCompetencyIds + "|" + competencies[i].CompetenceId.ToString();
                                else
                                    objSkill.SkillwiseCompetencyIds = competencies[i].CompetenceId.ToString();
                            }
                            lstSkills.Add(objSkill);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetSkillForOnboardedUser", ex.Message, ex.StackTrace));

            }

            return lstSkills;
        }
        public void UpdateUserSkill(int itemId, string competence, int userId, DateTime completiondate, bool isCompetenceChanged)
        {
            int skillId;
            int oldCompetence;

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List listUserSkill = context.Web.Lists.GetByTitle(AppConstant.UserSkills);
                    ListItemCreationInformation itemCreateInfoOnBoard = new ListItemCreationInformation();
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                         <Query>
                                             <Where>
                                                 <Eq>
                                                     <FieldRef Name='ID' />
                                                     <Value Type='Counter'>{0}</Value>
                                                 </Eq>
                                             </Where>
                                         </Query>
                                       </View>";
                    query.ViewXml = string.Format(query.ViewXml, itemId);

                    ListItemCollection listItemSkill = listUserSkill.GetItems(query);
                    context.Load(listItemSkill);
                    context.ExecuteQuery();

                    ListItem itemUserSkill = listItemSkill.SingleOrDefault();
                    FieldLookupValue userSkill = itemUserSkill["Skill"] as FieldLookupValue;
                    FieldLookupValue competenceLevel = itemUserSkill["CompetencyLevel"] as FieldLookupValue;
                    skillId = userSkill.LookupId;
                    oldCompetence = competenceLevel.LookupId;
                    itemUserSkill["CompetencyLevel"] = competence;
                    itemUserSkill.Update();
                    context.ExecuteQuery();

                    if (isCompetenceChanged == true)
                    {
                        CamlQuery queryUserPoint = new CamlQuery();
                        queryUserPoint.ViewXml = @"<View>
                                         <Query>
                                            <Where>
                                               <And>
                                                  <Eq>
                                                     <FieldRef Name='User1' LookupId='True' />
                                                     <Value Type='User'>{0}</Value>
                                                  </Eq>
                                                  <Eq>
                                                     <FieldRef Name='Skill' LookupId='True' />
                                                     <Value Type='Lookup'>{1}</Value>
                                                  </Eq>
                                               </And>
                                            </Where>
                                         </Query>
                                       </View>";

                        queryUserPoint.ViewXml = string.Format(queryUserPoint.ViewXml, userId, skillId);
                        List listUserPoint = context.Web.Lists.GetByTitle(AppConstant.UserPoints);
                        ListItemCollection listUserPointItems = listUserPoint.GetItems(queryUserPoint);
                        context.Load(listUserPointItems);
                        context.ExecuteQuery();

                        if (listUserPointItems != null && listUserPointItems.Count > 0)
                        {
                            ListItem itemUserPoint = listUserPointItems.SingleOrDefault();
                            itemUserPoint["CompetencyLevel"] = competence;
                            itemUserPoint["TrainingPoints"] = 0;
                            itemUserPoint["AssessmentPoints"] = 0;
                            itemUserPoint.Update();
                            context.ExecuteQuery();
                        }
                        AddSkillBasedTrainingAssessment(competence, Convert.ToInt32(skillId), Convert.ToInt32(userId), true, completiondate);
                        RemoveSkillBasedTrainingAssessment(context, oldCompetence, Convert.ToInt32(skillId), Convert.ToInt32(userId));
                    }
                    else
                    {
                        List<Training> trainings = GetTrainings(Convert.ToInt32(skillId), Convert.ToInt32(competence));
                        List<UserTraining> userTrainings = GetUserdetailsForTrainings(trainings, userId);

                        if (userTrainings != null && userTrainings.Count > 0)
                        {
                            List listUserTrainingMapping = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                            ListItemCreationInformation itemCreateInfoOnUserTraining = new ListItemCreationInformation();
                            CamlQuery queryusertraing = new CamlQuery();
                            queryusertraing.ViewXml = @"<View>
                                         <Query>
                                             <Where>
                                                 <Eq>
                                                     <FieldRef Name='ID' />
                                                     <Value Type='Counter'>{0}</Value>
                                                 </Eq>
                                             </Where>
                                         </Query>
                                       </View>";
                            queryusertraing.ViewXml = string.Format(queryusertraing.ViewXml, userTrainings[0].SkillId);

                            ListItemCollection listItemUserTraining = listUserTrainingMapping.GetItems(queryusertraing);

                            context.Load(listItemUserTraining);
                            context.ExecuteQuery();

                            ListItem itemUserTraining = listItemUserTraining.SingleOrDefault();
                            DateTime currentTimeKind = DateTime.SpecifyKind(completiondate, DateTimeKind.Local);
                            itemUserTraining["LastDayCompletion"] = currentTimeKind;
                            itemUserTraining.Update();
                            context.ExecuteQuery();
                        }

                        List<Assessment> assessments = GetAssessments(Convert.ToInt32(skillId), Convert.ToInt32(competence));

                        List<UserTraining> userassessment = GetUserdetailsForAssessments(assessments, userId);
                        if (userassessment != null && userassessment.Count > 0)
                        {
                            List listUserTrainingMapping = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
                            ListItemCreationInformation itemCreateInfoOnUserTraining = new ListItemCreationInformation();
                            CamlQuery queryusertraing = new CamlQuery();
                            queryusertraing.ViewXml = @"<View>
                                         <Query>
                                             <Where>
                                                 <Eq>
                                                     <FieldRef Name='ID' />
                                                     <Value Type='Counter'>{0}</Value>
                                                 </Eq>
                                             </Where>
                                         </Query>
                                       </View>";
                            queryusertraing.ViewXml = string.Format(queryusertraing.ViewXml, userassessment[0].SkillId);

                            ListItemCollection listItemUserTraining = listUserTrainingMapping.GetItems(queryusertraing);

                            context.Load(listItemUserTraining);
                            context.ExecuteQuery();

                            ListItem itemUserTraining = listItemUserTraining.SingleOrDefault();
                            DateTime currentTimeKind = DateTime.SpecifyKind(completiondate, DateTimeKind.Local);
                            itemUserTraining["LastDayCompletion"] = currentTimeKind;
                            itemUserTraining.Update();
                            context.ExecuteQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, UpdateUserSkill", ex.Message, ex.StackTrace));

            }

        }

        private List<UserTraining> GetUserdetailsForTrainings(List<Training> trainings, int userid)
        {
            List<UserTraining> lstUsersDetails = new List<UserTraining>();

            try
            {
                if (trainings.Count > 0)
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;

                        List userTrainingMapping = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                        CamlQuery query = new CamlQuery();
                        query.ViewXml = @"<View>
                                    <Query>
                                        <Where>                                           
                                            <And>
                                                
                                                <Eq>
                                                    <FieldRef Name='Training' LookupId='True' />
                                                    <Value Type='Lookup'>{0}</Value>
                                                </Eq>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{1}</Value>
                                                </Eq>
                                            </And>                                              
                                        </Where>
                                    </Query>
                            </View>";
                        query.ViewXml = string.Format(query.ViewXml, trainings[0].TrainingId, userid);
                        ListItemCollection collection = userTrainingMapping.GetItems(query);
                        context.Load(collection);
                        context.ExecuteQuery();
                        UserTraining objTraining = null;
                        if (collection != null && collection.Count > 0)
                        {
                            foreach (var item in collection)
                            {
                                objTraining = new UserTraining();

                                objTraining.IsTrainingCompleted = Convert.ToBoolean(item["IsTrainingCompleted"]);
                                objTraining.Employee = item["User1"] != null ? ((FieldLookupValue)(item["User1"])).LookupValue : "";
                                objTraining.LastDayCompletion = item["LastDayCompletion"].ToString();
                                objTraining.SkillId = item.Id;
                                lstUsersDetails.Add(objTraining);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserdetailsForTrainings", ex.Message, ex.StackTrace));

            }

            return lstUsersDetails;
        }

        private List<UserTraining> GetUserdetailsForAssessments(List<Assessment> assessments, int userid)
        {
            List<UserTraining> lstUsersDetails = new List<UserTraining>();

            try
            {
                if (assessments.Count > 0)
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;

                        List userAssessmentMapping = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
                        CamlQuery query = new CamlQuery();
                        query.ViewXml = @"<View>
                                    <Query>
                                        <Where>                                           
                                            <And>
                                                
                                                <Eq>
                                                    <FieldRef Name='Assessment' LookupId='True' />
                                                    <Value Type='Lookup'>{0}</Value>
                                                </Eq>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{1}</Value>
                                                </Eq>
                                            </And>                                              
                                        </Where>
                                    </Query>
                            </View>";
                        query.ViewXml = string.Format(query.ViewXml, assessments[0].AssessmentId, userid);
                        ListItemCollection collection = userAssessmentMapping.GetItems(query);
                        context.Load(collection);
                        context.ExecuteQuery();
                        UserTraining objTraining = null;
                        if (collection != null && collection.Count > 0)
                        {
                            foreach (var item in collection)
                            {
                                objTraining = new UserTraining();

                                objTraining.LastDayCompletion = item["LastDayCompletion"].ToString();
                                objTraining.SkillId = item.Id;
                                lstUsersDetails.Add(objTraining);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserdetailsForAssessments", ex.Message, ex.StackTrace));

            }

            return lstUsersDetails;
        }

        private int GetTrainingForAssessment(int assessmentId)
        {
            int trainingId = 0;

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    List lstAssessment = context.Web.Lists.GetByTitle(AppConstant.Assessments);
                    ListItem itemAssessment = lstAssessment.GetItemById(assessmentId);

                    context.Load(itemAssessment);
                    context.ExecuteQuery();

                    if (itemAssessment != null)
                    {
                        trainingId = ((FieldLookupValue)(itemAssessment["Training"])).LookupId;
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetTrainingForAssessment", ex.Message, ex.StackTrace));

            }

            return trainingId;
        }

        private int GetSkillForTraining(int trainingId)
        {
            int skillId = 0;

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    List listTrainingCourse = context.Web.Lists.GetByTitle(AppConstant.SkillCompetencyLevelTrainings);

                    ListItem itemTrainingCourse = listTrainingCourse.GetItemById(trainingId);

                    context.Load(itemTrainingCourse);
                    context.ExecuteQuery();

                    if (itemTrainingCourse != null)
                    {
                        skillId = ((FieldLookupValue)(itemTrainingCourse["Skill"])).LookupId;
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetSkillForTraining", ex.Message, ex.StackTrace));

            }
            return skillId;
        }



        public List<UserOnBoarding> GetOnBoardingDetailsReport(string status, bool isExcelDownload)
        {
            List<UserOnBoarding> lstUserOnBoarding = new List<UserOnBoarding>();
            using (ClientContext context = new ClientContext(CurrentSiteUrl))
            {
                #region Get list of all onboarded users
                context.Credentials = SPCredential;
                List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                CamlQuery query = new CamlQuery();
                query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='Status' LookupId='True' />
                                                    <Value Type='Text'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                query.ViewXml = string.Format(query.ViewXml, status);
                ListItemCollection collection = academyOnBoarding.GetItems(query);
                context.Load(collection);
                context.ExecuteQuery();
                #endregion

                if (collection != null && collection.Count > 0)
                {
                    #region Get List of all user skills
                    List userSkills = context.Web.Lists.GetByTitle(AppConstant.UserSkills);
                    CamlQuery querySkill = new CamlQuery();
                    querySkill.ViewXml = @"<View>
                                        <Query>
                                           <OrderBy>
                                              <FieldRef Name='ID' />
                                           </OrderBy>
                                        </Query>
                                      </View>";
                    ListItemCollection collectionSkill = userSkills.GetItems(querySkill);
                    context.Load(collectionSkill);
                    context.ExecuteQuery();
                    List<UserSkill> lstSkills = new List<UserSkill>();
                    UserSkill objSkill = null;
                    if (collectionSkill != null && collectionSkill.Count > 0)
                    {
                        foreach (var item in collectionSkill)
                        {
                            objSkill = new UserSkill();
                            objSkill.Id = item.Id;
                            objSkill.Skill = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "";
                            objSkill.SkillId = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupId : -1;
                            objSkill.Competence = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupValue : ""; //Convert.ToString(item["CompetencyLevel"]);
                            objSkill.CompetenceId = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupId : -1;
                            objSkill.UserId = ((FieldUserValue)(item["User1"])).LookupId;
                            lstSkills.Add(objSkill);
                        }
                    }
                    #endregion

                    #region Get List of all user trainings
                    ListItemCollection userTrainingCollection = null;
                    List lstUserTraining = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                    CamlQuery queryUserTraining = new CamlQuery();

                    queryUserTraining.ViewXml = @"<View><Query><Where><Eq><FieldRef Name='IsIncludeOnBoarding' /><Value Type='Boolean'>{0}</Value></Eq></Where></Query></View>";
                    queryUserTraining.ViewXml = string.Format(queryUserTraining.ViewXml, 1);
                    userTrainingCollection = lstUserTraining.GetItems(queryUserTraining);
                    context.Load(userTrainingCollection);
                    context.ExecuteQuery();
                    List<UserTraining> lstTrainings = new List<UserTraining>();
                    if (userTrainingCollection != null && userTrainingCollection.Count > 0)
                    {
                        foreach (var item in userTrainingCollection)
                        {
                            UserTraining objTraining = new UserTraining();
                            objTraining.CompletedDate = Convert.ToString(item["CompletedDate"]);
                            objTraining.IsIncludeOnBoarding = Convert.ToBoolean(item["IsIncludeOnBoarding"]);
                            objTraining.IsMandatory = Convert.ToBoolean(item["IsMandatory"]);
                            objTraining.IsTrainingActive = Convert.ToBoolean(item["IsTrainingActive"]);
                            objTraining.IsTrainingCompleted = Convert.ToBoolean(item["IsTrainingCompleted"]);
                            objTraining.LastDayCompletion = Convert.ToString(item["LastDayCompletion"]);
                            objTraining.SkillName = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "";
                            objTraining.SkillId= item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupId : 0;
                            objTraining.TrainingName = item["Training"] != null ? ((FieldLookupValue)(item["Training"])).LookupValue : "";
                            objTraining.TrainingId = item["Training"] != null ? ((FieldLookupValue)(item["Training"])).LookupId : 0;
                            objTraining.UserId = ((FieldUserValue)(item["User1"])).LookupId;

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
                    #endregion


                    #region Get List of User Assessments
                    List academyAssessment = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
                    CamlQuery queryUserAssessment = new CamlQuery();
                    queryUserAssessment.ViewXml = @"<View><Query><Where><Eq><FieldRef Name='IsIncludeOnBoarding' /><Value Type='Boolean'>{0}</Value></Eq></Where></Query></View>";

                    queryUserAssessment.ViewXml = string.Format(queryUserAssessment.ViewXml, 1);
                    ListItemCollection userAssessmentCollection = null;
                    userAssessmentCollection = academyAssessment.GetItems(queryUserAssessment);
                    context.Load(userAssessmentCollection);
                    context.ExecuteQuery();

                    List<UserAssessment> lstAssessments = new List<UserAssessment>();
                    if (userAssessmentCollection != null && userAssessmentCollection.Count > 0)
                    {
                        foreach (var item in userAssessmentCollection)
                        {
                            UserAssessment objUserAssessment = new UserAssessment();
                            objUserAssessment.CompletedDate = Convert.ToString(item["CompletedDate"]);
                            objUserAssessment.IsIncludeOnBoarding = Convert.ToBoolean(item["IsIncludeOnBoarding"]);
                            objUserAssessment.IsMandatory = Convert.ToBoolean(item["IsMandatory"]);
                            objUserAssessment.IsAssessmentActive = Convert.ToBoolean(item["IsAssessmentActive"]);
                            objUserAssessment.IsAssessmentComplete = Convert.ToBoolean(item["IsAssessmentComplete"]);
                            objUserAssessment.LastDayCompletion = Convert.ToString(item["LastDayCompletion"]);
                            objUserAssessment.SkillName= item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "";
                            objUserAssessment.SkillId = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupId : 0;
                            objUserAssessment.TrainingName = item["Training"] != null ? ((FieldLookupValue)(item["Training"])).LookupValue : "";
                            objUserAssessment.TrainingId = item["Training"] != null ? ((FieldLookupValue)(item["Training"])).LookupId : 0;
                            objUserAssessment.TrainingAssessment = item["Assessment"] != null ? ((FieldLookupValue)(item["Assessment"])).LookupValue : "";
                            objUserAssessment.TrainingAssessmentId = item["Assessment"] != null ? ((FieldLookupValue)(item["Assessment"])).LookupId : 0;
                            objUserAssessment.UserId = ((FieldUserValue)(item["User1"])).LookupId;

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
                    #endregion

                    foreach (var item in collection)
                    {
                        UserOnBoarding objUserOnBoarding = new UserOnBoarding();
                        objUserOnBoarding.CurrentSkill = item["Skill"] != null ? ((FieldLookupValue)(item["Skill"])).LookupValue : "";
                        objUserOnBoarding.CurrentGEO = item["GEO"] != null ? ((FieldLookupValue)(item["GEO"])).LookupValue : "";
                        objUserOnBoarding.CurrentCompetance = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupValue : "";
                        objUserOnBoarding.CurrentStatus = item["Status"] != null ? Convert.ToString(item["Status"]) : "";
                        objUserOnBoarding.CurrentBGVStatus = item["BGVStatus"] != null ? Convert.ToString(item["BGVStatus"]) : "";
                        objUserOnBoarding.CurrentProfileSharing = item["ProfileSharing"] != null ? Convert.ToString(item["ProfileSharing"]) : "";
                        objUserOnBoarding.Email = ((FieldUserValue)(item["User1"])).Email;
                        objUserOnBoarding.UserId = ((FieldUserValue)(item["User1"])).LookupId;
                        objUserOnBoarding.Name = ((FieldUserValue)(item["User1"])).LookupValue;
                        objUserOnBoarding.ProjectId = item["Project"] != null ? Convert.ToInt32(((FieldLookupValue)(item["Project"])).LookupId) : -1;
                        objUserOnBoarding.ProjectName = item["Project"] != null ? Convert.ToString(((FieldLookupValue)(item["Project"])).LookupValue) : "";


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
            }
            return lstUserOnBoarding;
        }

        public List<Competence> GetCompetenciesBySkillId(int SkillID)
        {
            List<Competence> competencies = new List<Competence>();

            Competence all = new Competence
            {
                CompetenceId = 0,
                CompetenceName = "All"
            };

            competencies.Add(all);

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    if (SkillID > 0)
                    {
                        string query = @"<View>
                                        <Query>
                                            <Where>                                               
                                                <Eq>
                                                    <FieldRef Name='Skill' LookupId='True' />
                                                    <Value Type='Lookup'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                        query = string.Format(query, SkillID);

                        ListItemCollection lstSkillCompetency = SharePointUtil.GetListItems(AppConstant.SkillCompetencyLevels, context, null, query);

                        if (lstSkillCompetency != null & lstSkillCompetency.Count > 0)
                        {
                            Competence c = null;
                            foreach (ListItem item in lstSkillCompetency)
                            {
                                c = new Competence()
                                {
                                    CompetenceId = item.Id,
                                    CompetenceName = Convert.ToString(item["Title"])

                                };
                                competencies.Add(c);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetCompetenciesBySkillId", ex.Message, ex.StackTrace));

            }

            return competencies;
        }

        public List<Competence> GetCompetenciesBySkillName(string name)
        {
            List<Competence> competencies = new List<Competence>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    string skillquery = @"<View>
                                        <Query>
                                            <Where>                                               
                                                <Eq>
                                                    <FieldRef Name='Title'  />
                                                    <Value Type='Text'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";

                    skillquery = string.Format(skillquery, name);
                    ListItemCollection lstSkill = SharePointUtil.GetListItems(AppConstant.Skills, context, null, skillquery);

                    if (lstSkill != null & lstSkill.Count > 0)
                    {
                        int Id = lstSkill[0].Id;


                        if (Id > 0)
                        {
                            string query = @"<View>
                                        <Query>
                                            <Where>                                               
                                                <Eq>
                                                    <FieldRef Name='Skill' LookupId='True' />
                                                    <Value Type='Lookup'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                            query = string.Format(query, Id);

                            ListItemCollection lstSkillCompetency = SharePointUtil.GetListItems(AppConstant.SkillCompetencyLevels, context, null, query);

                            if (lstSkillCompetency != null & lstSkillCompetency.Count > 0)
                            {
                                Competence c = null;
                                foreach (ListItem item in lstSkillCompetency)
                                {
                                    c = new Competence()
                                    {
                                        CompetenceId = item.Id,
                                        CompetenceName = Convert.ToString(item["Title"])

                                    };
                                    competencies.Add(c);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetCompetenciesBySkillName", ex.Message, ex.StackTrace));

            }

            return competencies;
        }
        public void RemoveUserRole(int roleId, string userId)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, userId);
                    ListItemCollection collection = academyOnBoarding.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();

                    if (collection != null && collection.Count > 0)
                    {
                        ListItem item = collection.SingleOrDefault();
                        FieldLookupValue[] roles = ((FieldLookupValue[])item["Roles"]);
                        List<FieldLookupValue> roleLookupValueList = new List<FieldLookupValue>();
                        for (int j = 0; j < roles.Length; j++)
                        {
                            if (roles[j].LookupId != roleId)
                                roleLookupValueList.Add(new FieldLookupValue { LookupId = roles[j].LookupId });
                        }

                        item["Roles"] = roleLookupValueList;
                        item.Update();
                        context.ExecuteQuery();
                        RemoveRoleBasedTraining(context, roleId, Convert.ToInt32(userId));
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, RemoveUserSkill", ex.Message, ex.StackTrace));
            }
        }
        public void RemoveUserSkill(int itemId, string userId)
        {
            int skillId;
            int competenceId;
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    List listUserSkill = context.Web.Lists.GetByTitle(AppConstant.UserSkills);
                    ListItemCreationInformation itemCreateInfoOnBoard = new ListItemCreationInformation();
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='ID' />
                                                    <Value Type='Counter'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, itemId);

                    ListItemCollection listItemSkill = listUserSkill.GetItems(query);
                    context.Load(listItemSkill);
                    context.ExecuteQuery();

                    ListItem itemUserSkill = listItemSkill.SingleOrDefault();
                    FieldLookupValue userSkill = itemUserSkill["Skill"] as FieldLookupValue;
                    FieldLookupValue competenceLevel = itemUserSkill["CompetencyLevel"] as FieldLookupValue;
                    skillId = userSkill.LookupId;
                    competenceId = competenceLevel.LookupId;

                    /////////Delete record from UserSkill list/////////
                    listUserSkill.GetItemById(itemId).DeleteObject();
                    context.ExecuteQuery();


                    /////////Delete record from UserPoint list/////////
                    CamlQuery queryUserPoint = new CamlQuery();
                    queryUserPoint.ViewXml = @"<View>
                                        <Query>
                                           <Where>
                                              <And>
                                                 <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                 </Eq>
                                                 <And> 
                                                     <Eq>
                                                        <FieldRef Name='Skill' LookupId='True' />
                                                        <Value Type='Lookup'>{1}</Value>
                                                     </Eq>
                                                     <Eq>
										               <FieldRef Name='CompetencyLevel' LookupId='True'/>
										               <Value Type='Lookup'>{2}</Value>
										            </Eq>
                                                 </And>
                                              </And>
                                           </Where>
                                        </Query>
                                      </View>";

                    queryUserPoint.ViewXml = string.Format(queryUserPoint.ViewXml, userId, skillId, competenceId);
                    List listUserPoint = context.Web.Lists.GetByTitle(AppConstant.UserPoints);
                    ListItemCollection listUserPointItems = listUserPoint.GetItems(queryUserPoint);
                    context.Load(listUserPointItems);
                    context.ExecuteQuery();
                    if (listUserPointItems != null && listUserPointItems.Count > 0)
                    {
                        foreach (ListItem item in listUserPointItems)
                        {
                            listUserPoint.GetItemById(item.Id).DeleteObject();
                        }
                    }
                    context.ExecuteQuery();

                    ///////Remove training and assessments for the skill and competency/////////
                    RemoveSkillBasedTrainingAssessment(context, competenceId, Convert.ToInt32(skillId), Convert.ToInt32(userId));
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, RemoveUserSkill", ex.Message, ex.StackTrace));

            }
        }

        private ListItemCollection GetUserTrainings(int userId)
        {
            ListItemCollection collection = null;

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                    CamlQuery query = new CamlQuery();

                    query.ViewXml = @"<View>
                                    <Query>
                                        <Where>                                           
                                            <And>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                                    <Eq>
                                                         <FieldRef Name='IsIncludeOnBoarding' />
                                                         <Value Type='Boolean'>{1}</Value>
                                                    </Eq>                                                    
                                                </And>                                            
                                        </Where>
                                    </Query>
                            </View>";
                    query.ViewXml = string.Format(query.ViewXml, userId, 1);

                    collection = academyOnBoarding.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserTrainings", ex.Message, ex.StackTrace));

            }

            return collection;
        }

        private ListItemCollection GetUserAssessments(int userId)
        {
            ListItemCollection collection = null;

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List academyAssessment = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
                    CamlQuery query = new CamlQuery();

                    query.ViewXml = @"<View>
                                    <Query>
                                        <Where>                                           
                                            <And>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                                    <Eq>
                                                         <FieldRef Name='IsIncludeOnBoarding' />
                                                         <Value Type='Boolean'>{1}</Value>
                                                    </Eq>                                                    
                                                </And>                                            
                                        </Where>
                                    </Query>
                            </View>";

                    query.ViewXml = string.Format(query.ViewXml, userId, 1);

                    collection = academyAssessment.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserAssessments", ex.Message, ex.StackTrace));

            }

            return collection;
        }

        public void CacheConfig()
        {
            if (HttpContext.Current.Session["AcademyConfig"] == null)
            {
                ListItemCollection lstItemsConfig = SharePointUtil.GetListItems(AppConstant.AcademyConfig, null, string.Empty);

                if (lstItemsConfig != null & lstItemsConfig.Count > 0)
                {
                    HttpContext.Current.Session["AcademyConfig"] = lstItemsConfig;
                }
            }
        }

        public WikiPolicyDocuments GetWikiDocumentTree(HttpServerUtilityBase Server, string folder)
        {
            WikiPolicyDocuments poldocs = new WikiPolicyDocuments();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    if (!string.IsNullOrEmpty(folder))
                    {
                        string folderURL = "/sites/Academy/STATE/OnBoarding/" + folder;

                        List<WikiDocuments> TrainingDocFirst = new List<WikiDocuments>();

                        string camlQuery = "@<View Scope='RecursiveAll'></View>";

                        SharePointDAL dal = new SharePointDAL();
                        ListItemCollection lstItemsDoc = dal.GetWikiDocumentItems(AppConstant.WikiDocuments, context, null, camlQuery);
                        if (lstItemsDoc != null & lstItemsDoc.Count > 0)
                        {

                            foreach (ListItem item in lstItemsDoc)
                            {
                                if (Convert.ToString(item["FileRef"]).Contains(folderURL))
                                {
                                    var perms = item.EffectiveBasePermissions.Has(PermissionKind.OpenItems); //Permission Trimmed
                                    if (perms)
                                    {
                                        WikiDocuments wiki = new WikiDocuments();
                                        wiki.ID = item.Id;
                                        wiki.ParentFolder = Server.UrlDecode(item["EncodedAbsUrl"].ToString().Split('/')[item["EncodedAbsUrl"].ToString().Split('/').Length - 2]);
                                        wiki.DocumentURL = item["EncodedAbsUrl"].ToString();
                                        wiki.ParentFolderURL = wiki.DocumentURL.Remove(wiki.DocumentURL.LastIndexOf("/"));
                                        if (item.FileSystemObjectType == FileSystemObjectType.Folder)
                                        {
                                            wiki.IsFolder = true;
                                            wiki.DocumentName = item.DisplayName;
                                        }
                                        else
                                        {
                                            wiki.DocumentName = item.DisplayName + "." + item["EncodedAbsUrl"].ToString().Split('/').Last().Split('.').Last().ToString();
                                            wiki.IsFolder = false;
                                        }

                                        TrainingDocFirst.Add(wiki);
                                    }
                                }
                            }
                            poldocs.ListOfWikiDoc = GetChild(TrainingDocFirst);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetWikiDocumentTree", ex.Message, ex.StackTrace));

            }
            return poldocs;
        }

        public string GetUserCompetencyLabel(int userid)
        {
            string competencyLabel = string.Empty;
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                    CamlQuery queryCompetency = new CamlQuery();
                    queryCompetency.ViewXml = @"<View>
                                                    <Query>
                                                        <Where>
                                                            <Eq>
                                                                <FieldRef Name='User1' LookupId='True' />
                                                                <Value Type='User'>{0}</Value>
                                                            </Eq>
                                                        </Where>
                                                    </Query>
                                                  </View>";
                    queryCompetency.ViewXml = string.Format(queryCompetency.ViewXml, userid);
                    ListItemCollection collection = academyOnBoarding.GetItems(queryCompetency);
                    context.Load(collection);
                    context.ExecuteQuery();

                    if (collection != null && collection.Count > 0)
                    {
                        ListItem item = collection.SingleOrDefault();
                        var c = item["CompetencyLevel"] as FieldLookupValue;
                        if (c != null)
                        {
                            competencyLabel = c.LookupValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserCompetancyLabel", ex.Message, ex.StackTrace));

            }
            return competencyLabel;
        }

        public List<AcademyJoinersCompletion> GetCurrentUserAssessments(string listName, int? Id, bool updateAttempt)
        {
            List<Assessment> assessments = GetAllAssessments();
            Hashtable hshAllAssessments = new Hashtable();
            foreach (Assessment assessment in assessments)
            {
                hshAllAssessments.Add(assessment.AssessmentId, assessment);
            }

            List<AcademyJoinersCompletion> trainingDetails = new List<AcademyJoinersCompletion>();
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List lstUserAssessment = context.Web.Lists.GetByTitle(listName);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <And>
                                                    <Eq>
                                                        <FieldRef Name='User1' LookupId='True' />
                                                        <Value Type='User'>{0}</Value>
                                                    </Eq>
                                                    <Eq>
                                                        <FieldRef Name='IsAssessmentActive' />
                                                        <Value Type='Boolean'>{1}</Value>
                                                    </Eq>
                                                </And>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, CurrentUser.SPUserId, 1);
                    if (Id != null)
                    {
                        query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='ID'  />
                                                    <Value Type='Counter'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                        query.ViewXml = string.Format(query.ViewXml, Id);
                    }
                    ListItemCollection collection = lstUserAssessment.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();
                    foreach (ListItem item in collection)
                    {
                        FieldLookupValue trainingModule = (FieldLookupValue)item.FieldValues["Training"];
                        if (trainingModule != null)
                        {
                            var objTraining = new AcademyJoinersCompletion();
                            objTraining.Id = Convert.ToInt32(item.FieldValues["ID"]);
                            objTraining.Title = Convert.ToString(item.FieldValues["Title"]); ;
                            FieldLookupValue trainingCourse = (FieldLookupValue)item.FieldValues["Skill"];
                            if (trainingCourse != null)
                            {
                                objTraining.TrainingCourseLookUpId = trainingCourse.LookupId;
                                objTraining.TrainingCourseLookUpText = trainingCourse.LookupValue;
                            }
                            objTraining.TrainingModuleLookUpId = trainingModule.LookupId;
                            objTraining.TrainingModuleLookupText = trainingModule.LookupValue;
                            FieldLookupValue trainingAssessment = (FieldLookupValue)item["Assessment"];
                            if (trainingAssessment != null)
                            {
                                objTraining.TrainingAssessmentLookUpId = trainingAssessment.LookupId;
                                objTraining.TrainingAssessmentLookUpText = trainingAssessment.LookupValue;

                                if (hshAllAssessments.ContainsKey(trainingAssessment.LookupId))
                                {
                                    Assessment assessment = (Assessment)hshAllAssessments[trainingAssessment.LookupId];
                                    bool trainingLink = false;
                                    if (!string.IsNullOrEmpty(assessment.AssessmentLink))
                                        trainingLink = true;

                                    objTraining.TrainingLink = assessment.AssessmentLink;
                                    objTraining.IsTrainingLink = trainingLink;
                                    objTraining.TrainingAssessmentTimeInMins = assessment.AssessmentTimeInMins;
                                }
                            }

                            objTraining.IsMandatory = Convert.ToBoolean(item.FieldValues["IsMandatory"]);
                            objTraining.LastDayCompletion = Convert.ToDateTime(item.FieldValues["LastDayCompletion"]).ToLocalTime();
                            objTraining.CompletedDate = Convert.ToDateTime(item.FieldValues["CompletedDate"]).ToLocalTime();
                            objTraining.CertificateMailSent = Convert.ToBoolean(item.FieldValues["CertificateMailSent"]); ;
                            objTraining.Attempts = Convert.ToInt32(item.FieldValues["NoOfAttempt"]);
                            objTraining.MaxAttempts = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxAttempts"]) == -1 ? int.MaxValue : Convert.ToInt32(ConfigurationManager.AppSettings["MaxAttempts"]);
                            objTraining.AssessmentStatus = Convert.ToBoolean(item.FieldValues["IsAssessmentComplete"]); //Assessment Based - SKB

                            objTraining.CompletionDate = Convert.ToDateTime(item["LastDayCompletion"].ToString()).ToShortDateString();

                            if (!objTraining.IsTrainingLink)
                            {
                                try
                                {
                                    if (string.IsNullOrEmpty(Convert.ToString(item.FieldValues["MarksInPercentage"])))
                                    {
                                        var questions = GetEachQuestionDetails(AppConstant.AcademyAssessment, objTraining.TrainingAssessmentLookUpId);
                                        if (questions.Count != 0)
                                        {
                                            if (questions.Count > AppConstant.MaxQueForAssessment)
                                            {
                                                objTraining.MarksSecured = (Convert.ToInt32(item.FieldValues["MarksObtained"]) * 100) / (AppConstant.MaxQueForAssessment * Convert.ToInt32(questions[0].Marks));
                                            }
                                            else
                                            {
                                                objTraining.MarksSecured = (Convert.ToInt32(item.FieldValues["MarksObtained"]) * 100) / (questions.Count * Convert.ToInt32(questions[0].Marks));
                                            }
                                        }
                                        else
                                        {
                                            objTraining.TrainingLink = "NOLINK";
                                        }
                                    }
                                    else
                                    {
                                        objTraining.MarksSecured = Convert.ToInt32(item.FieldValues["MarksInPercentage"]);
                                    }
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                objTraining.MarksSecured = Convert.ToInt32(item.FieldValues["MarksInPercentage"]);
                            }

                            trainingDetails.Add(objTraining);
                        }
                        if (updateAttempt)
                        {
                            ListItem assesmentItem = collection.SingleOrDefault();
                            int attempts = trainingDetails.SingleOrDefault().Attempts;
                            attempts++;
                            assesmentItem["NoOfAttempt"] = attempts;
                            assesmentItem.Update();
                            context.ExecuteQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetCurrentUserAssessments", ex.Message, ex.StackTrace));

            }

            return trainingDetails;
        }

        public List<UserTrainingDetail> GetTrainingItems()
        {
            try
            {
                return GetTrainingDetails();
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetTrainingItems", ex.Message, ex.StackTrace));
                return null;
            }
        }

        private List<UserTrainingDetail> GetTrainingDetails()  //, bool isNewJoinee
        {
            Hashtable hshSkillCompetencyLevelTrainings = GetAllSkillCompetencyLevelTrainings();


            List<UserTrainingDetail> trainingInfo = new List<UserTrainingDetail>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    UserTrainingDetail trainingDetails = new UserTrainingDetail();
                    Modules trainingModules = new Modules();
                    ListItemCollection items = null;
                    Microsoft.SharePoint.Client.Web currentWeb = context.Web;
                    List list = currentWeb.Lists.GetByTitle(AppConstant.UserTrainingMapping);

                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, CurrentUser.SPUserId);
                    items = list.GetItems(query);
                    context.Load(items);
                    context.Load(currentWeb);
                    context.ExecuteQuery();

                    string docUrl = string.Empty;

                    foreach (ListItem item in items)
                    {
                        try
                        {
                            UserTrainingDetail training = new UserTrainingDetail();
                            training.IsLink = false;
                            training.LinkUrl = null;

                            SkillCompetencyLevelTraining _SkillCompetencyLevelTraining = null;

                            var childIdFieldCourse = item["Skill"] as FieldLookupValue;
                            var childIdFieldModule = item["Training"] as FieldLookupValue;
                            if (childIdFieldModule != null)
                            {
                                if (hshSkillCompetencyLevelTrainings.ContainsKey(Convert.ToString(childIdFieldModule.LookupId)))
                                {
                                    _SkillCompetencyLevelTraining = (SkillCompetencyLevelTraining)hshSkillCompetencyLevelTrainings[Convert.ToString(childIdFieldModule.LookupId)];
                                }
                            }
                            training.SkillName = childIdFieldCourse.LookupValue;
                            if (childIdFieldModule != null)
                            {
                                training.TrainingName = childIdFieldModule.LookupValue;
                            }

                            if (_SkillCompetencyLevelTraining != null)
                            {
                                training.LinkUrl = _SkillCompetencyLevelTraining.TrainingLink;
                                if (!string.IsNullOrEmpty(_SkillCompetencyLevelTraining.TrainingLink))
                                {
                                    training.IsLink = true;
                                }
                                else
                                {
                                    training.IsLink = false;
                                    training.DocumentUrl = _SkillCompetencyLevelTraining.TrainingDocument;
                                }
                            }
                            //Added for Mandatory / Suggested
                            training.Mandatory = false;
                            if (Convert.ToBoolean(item["IsMandatory"]) == true)
                            {
                                training.Mandatory = true;
                            }

                            training.IsTrainingCompleted = false;
                            if (item["IsTrainingCompleted"] != null && item["IsTrainingCompleted"].ToString().ToUpper() == "TRUE")
                            {
                                training.IsTrainingCompleted = true;
                            }
                            training.LastDayToComplete = Convert.ToDateTime(item["LastDayCompletion"]);

                            training.status = Utilities.GetTraningStatus(training.IsTrainingCompleted, training.NoOfAttempts, training.LastDayToComplete);
                            training.bgColor = Utilities.GetTrainingColor(training.status);
                            training.CompletionDate = Convert.ToDateTime(item["LastDayCompletion"].ToString()).ToShortDateString();
                            training.TrainingType = TrainingType.SkillTraining;
                            trainingInfo.Add(training);
                        }
                        catch (Exception ex)
                        {
                            UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                            LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetTrainingDetails", ex.Message, ex.StackTrace));
                            Utilities.LogToEventVwr(ex.StackTrace.ToString(), 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetTrainingDetails", ex.Message, ex.StackTrace));

            }
            GetUserRoleBasedTraining(ref trainingInfo, CurrentUser.SPUserId);

            return trainingInfo;
        }

        public void GetUserRoleBasedTraining(ref List<UserTrainingDetail> trainings, int userid)
        {
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    string query = @"<View>
							<Query>
								<Where>                                               
									<Eq>
                                        <FieldRef Name='User1' LookupId='True' />
                                        <Value Type='User'>{0}</Value>
                                    </Eq>
								</Where>
							</Query>
						  </View>";
                    query = string.Format(query, userid);
                    ListItemCollection listOfTrainings = SharePointUtil.GetListItems(AppConstant.UserRoleTraining, context, null, query);

                    if (listOfTrainings != null && listOfTrainings.Count() > 0)
                    {
                        foreach (ListItem item in listOfTrainings)
                        {
                            UserTrainingDetail objUserTrainingDetail = new UserTrainingDetail();
                            List<RoleTraining> allRoleTrainings = new List<RoleTraining>();
                            RoleTraining filteredRoleBasedTraining = new RoleTraining();
                            var trainingField = item["RoleBasedTraining"] as FieldLookupValue;
                            var userRolesLookUp = item["Roles"] as FieldLookupValue[];
                            if (userRolesLookUp != null)
                            {
                                var roleAssignedToUser = userRolesLookUp.FirstOrDefault();
                                allRoleTrainings = GetRoleTrainings(roleAssignedToUser.LookupId);
                                objUserTrainingDetail.SkillName = roleAssignedToUser.LookupValue;
                            }
                            if (trainingField != null)
                            {
                                filteredRoleBasedTraining = allRoleTrainings.Where(training => training.TrainingId == Convert.ToInt32(trainingField.LookupId)).FirstOrDefault();
                            }
                            objUserTrainingDetail.Mandatory = false;
                            if (filteredRoleBasedTraining.IsMandatory == true)
                            {
                                objUserTrainingDetail.Mandatory = true;
                            }
                            objUserTrainingDetail.Id = Convert.ToInt32(item["ID"]);
                            objUserTrainingDetail.TrainingName = filteredRoleBasedTraining.TrainingName != null ? filteredRoleBasedTraining.TrainingName : "";
                            objUserTrainingDetail.TrainingId = filteredRoleBasedTraining.TrainingId;
                            objUserTrainingDetail.ModuleDesc = filteredRoleBasedTraining.Description;
                            objUserTrainingDetail.IsLink = false;
                            objUserTrainingDetail.LinkUrl = null;
                            objUserTrainingDetail.LinkUrl = filteredRoleBasedTraining.URL;
                            if (!string.IsNullOrEmpty(filteredRoleBasedTraining.URL))
                            {
                                objUserTrainingDetail.IsLink = true;
                            }
                            else
                            {
                                objUserTrainingDetail.IsLink = false;
                                objUserTrainingDetail.DocumentUrl = filteredRoleBasedTraining.URL;
                            }
                            objUserTrainingDetail.LastDayToComplete = Convert.ToDateTime(item["LastDayCompletion"]);
                            objUserTrainingDetail.CompletionDate = Convert.ToDateTime(item["LastDayCompletion"].ToString()).ToShortDateString();
                            if (item["IsTrainingCompleted"].ToString().ToUpper() == "TRUE")
                                objUserTrainingDetail.IsTrainingCompleted = true;
                            else
                                objUserTrainingDetail.IsTrainingCompleted = false;
                            objUserTrainingDetail.status = Utilities.GetTraningStatus(objUserTrainingDetail.IsTrainingCompleted, objUserTrainingDetail.NoOfAttempts, objUserTrainingDetail.LastDayToComplete);
                            objUserTrainingDetail.bgColor = Utilities.GetTrainingColor(objUserTrainingDetail.status);
                            objUserTrainingDetail.TrainingType = TrainingType.RoleTraining;
                            trainings.Add(objUserTrainingDetail);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager loggeduser = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, loggeduser.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserRoleBasedTraining", ex.Message, ex.StackTrace));
            }
        }

        private Hashtable GetAllSkillCompetencyLevelTrainings()
        {
            Hashtable hsh = new Hashtable();
            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;
                    context.Load(context.Web);
                    context.ExecuteQuery();
                    var domainUrl = context.Web.Url.ToString().Remove(context.Web.Url.ToString().IndexOf("/sites"));
                    ListItemCollection items = SharePointUtil.GetListItems(AppConstant.SkillCompetencyLevelTrainings, context, null, string.Empty);
                    foreach (ListItem item in items)
                    {
                        int _ID = Convert.ToInt32(item["ID"].ToString());
                        string _TrainingLink = null;
                        string _TrainingDocument = null;

                        if (item.FieldValues["TrainingLink"] != null)
                        {
                            if (string.IsNullOrEmpty(item.FieldValues["TrainingLink"].ToString()) == false)
                            {
                                var link = ((FieldUrlValue)(item.FieldValues["TrainingLink"]));
                                _TrainingLink = link != null ? link.Url : string.Empty;
                            }
                        }

                        if (string.IsNullOrEmpty(_TrainingLink))
                        {
                            if (item.FieldValues["TrainingDocument"] != null)
                            {
                                var docURL = item.FieldValues["TrainingDocument"];
                                _TrainingDocument = docURL.ToString();
                            }
                        }

                        SkillCompetencyLevelTraining _SkillCompetencyLevelTraining = new SkillCompetencyLevelTraining()
                        {
                            ID = _ID,
                            TrainingLink = _TrainingLink,
                            TrainingDocument = _TrainingDocument
                        };

                        hsh.Add(_ID.ToString(), _SkillCompetencyLevelTraining);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetAllSkillCompetencyLevelTrainings", ex.Message, ex.StackTrace));

            }

            return hsh;
        }
        public List<UserSkillDetail> GetUserTrainingsDetails(string SPlistName)
        {
            List<UserSkillDetail> trainingCourses = new List<UserSkillDetail>();

            UserManager user = (UserManager)System.Web.HttpContext.Current.Session["CurrentUser"];

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    List<UserTrainingDetail> traingModules = GetTrainingDetails();


                    if (traingModules != null && traingModules.Count > 0)
                    {
                        trainingCourses = GetCouseWiseTrainingModules(traingModules);

                        return trainingCourses.OrderBy(x => x.LastDayToComplete).ToList();
                    }

                }
            }
            catch (Exception ex)
            {
                UserManager loggeduser = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, loggeduser.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserTrainingsDetails", ex.Message, ex.StackTrace));

            }
            return trainingCourses;
        }


        private List<UserSkillDetail> GetCouseWiseTrainingModules(List<UserTrainingDetail> trainingModuleList)
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

        public List<Result> GetSearch(string searchItem)
        {
            List<Result> lstresult = new List<Result>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    context.Load(context.Web, w => w.Url);
                    context.ExecuteQuery();
                    if (!string.IsNullOrEmpty(searchItem))
                    {

                        var keywordQuery = new KeywordQuery(context)
                        {
                            QueryText = "(FileExtension:doc OR FileExtension:docx OR FileExtension:xls OR FileExtension:xlsx OR FileExtension:ppt OR FileExtension:pptx OR FileExtension:pdf) (IsDocument:\"True\" OR contentclass:\"STS_ListItem\") (Path:" + context.Web.Url + ") {" + searchItem + "}"
                        };
                        keywordQuery.TrimDuplicates = true;
                        SearchExecutor searchExecutor = new SearchExecutor(context);
                        ClientResult<ResultTableCollection> results = searchExecutor.ExecuteQuery(keywordQuery);
                        context.ExecuteQuery();

                        if (results != null)
                        {
                            foreach (var resultRow in results.Value[0].ResultRows)
                            {
                                if (resultRow != null && resultRow["Title"] != null)
                                {
                                    Result result = new Result();
                                    result.ResultName = resultRow["Title"].ToString() + "." + resultRow["FileType"].ToString();
                                    result.ResultAuthor = resultRow["Author"].ToString();
                                    result.ResultModified = Convert.ToDateTime(resultRow["LastModifiedTime"].ToString()).ToShortDateString();
                                    result.ResultHighlights = resultRow["HitHighlightedSummary"].ToString().Replace("<ddd/>", "......");
                                    result.ResultSource = resultRow["Path"].ToString();
                                    lstresult.Add(result);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetSearch", ex.Message, ex.StackTrace));

            }

            return lstresult;
        }

        public List<QuestionDetail> GetEachQuestionDetails(string listName, int p2)
        {
            List<QuestionDetail> allQuestions = new List<QuestionDetail>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    QuestionDetail eachQuestionDetails = null;
                    ListItemCollection items = null;
                    Microsoft.SharePoint.Client.Web currentWeb = context.Web;
                    List list = currentWeb.Lists.GetByTitle(listName);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>    
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='Assessment' LookupId='TRUE' />
                                                    <Value Type=â€Lookupâ€>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                    </View>";
                    query.ViewXml = string.Format(query.ViewXml, p2);
                    items = list.GetItems(query);
                    context.Load(items, item => item.IncludeWithDefaultProperties(i => i.FieldValuesAsText));
                    context.ExecuteQuery();
                    if (items.Count > 0)
                    {
                        foreach (ListItem item in items)
                        {
                            eachQuestionDetails = new QuestionDetail();
                            string multitextValue = item.FieldValuesAsText["Question"];

                            eachQuestionDetails.QuestionTitle = multitextValue;
                            eachQuestionDetails.Option1 = Convert.ToString(item["Option1"]);
                            eachQuestionDetails.Option2 = Convert.ToString(item["Option2"]);
                            eachQuestionDetails.Option3 = Convert.ToString(item["Option3"]);
                            eachQuestionDetails.Option4 = Convert.ToString(item["Option4"]);
                            if (item["Option5"] != null)
                            {
                                if (item["Option5"].ToString().Length > 0)
                                    eachQuestionDetails.Option5 = item["Option5"].ToString();


                            }

                            int CorrectOptionSequence = Convert.ToInt32(item["CorrectOptionSequence"]);
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
        private List<UserTrainingDetail> GetSkillBasedTrainingsList(string SPlistName, int userId)
        {
            List<UserTrainingDetail> trainingsLst = new List<UserTrainingDetail>();
            try
            {

                ListItemCollection items = null;

                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    Microsoft.SharePoint.Client.Web currentWeb = context.Web;
                    List list = currentWeb.Lists.GetByTitle(SPlistName);
                    CamlQuery query = new CamlQuery();

                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                            <And>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                               <Eq>
                                                    <FieldRef Name='IsTrainingActive' />
                                                    <Value Type='Boolean'>1</Value>
                                               </Eq>
                                            </And>
                                            </Where>
                                        </Query>
                                      </View>";

                    query.ViewXml = string.Format(query.ViewXml, userId);
                    items = list.GetItems(query);
                    context.Load(items);
                    context.ExecuteQuery();

                    if (items != null && items.Count > 0)
                    {
                        ArrayList defaultTrainingIds = new ArrayList();
                        ArrayList defaultAssessmentIds = new ArrayList();
                        BuildDefaultTrainingAssessmentArray(defaultTrainingIds, defaultAssessmentIds);

                        foreach (ListItem item in items)
                        {
                            var childIdFieldModule = item["Training"] as FieldLookupValue;

                            if (!defaultTrainingIds.Contains(childIdFieldModule.LookupId))
                            {
                                UserTrainingDetail training = new UserTrainingDetail();

                                var childIdFieldCourse = item["Skill"] as FieldLookupValue;
                                if (childIdFieldCourse != null)
                                {
                                    training.SkillName = childIdFieldCourse.LookupValue;
                                    training.SkillId = childIdFieldCourse.LookupId;
                                    if (childIdFieldModule != null)
                                    {
                                        training.TrainingName = childIdFieldModule.LookupValue;
                                    }
                                    training.IsTrainingCompleted = false;
                                    if (item["IsTrainingCompleted"] != null && item["IsTrainingCompleted"].ToString().ToUpper() == "TRUE")
                                    {
                                        training.IsTrainingCompleted = true;
                                    }
                                    training.LastDayToComplete = Convert.ToDateTime(item["LastDayCompletion"]);
                                    training.status = Utilities.GetTraningStatus(training.IsTrainingCompleted, training.NoOfAttempts, training.LastDayToComplete);
                                    training.bgColor = Utilities.GetTrainingColor(training.status);
                                    trainingsLst.Add(training);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetSkillBasedTrainingsList", ex.Message, ex.StackTrace));

            }
            return trainingsLst;
        }
        private List<UserTrainingDetail> GetTrainingsList(string SPlistName, int userId)
        {
            List<UserTrainingDetail> trainingsLst = new List<UserTrainingDetail>();
            try
            {

                ListItemCollection items = null;

                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    Microsoft.SharePoint.Client.Web currentWeb = context.Web;
                    List list = currentWeb.Lists.GetByTitle(SPlistName);
                    CamlQuery query = new CamlQuery();

                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                            <And>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                               <Eq>
                                                    <FieldRef Name='IsTrainingActive' />
                                                    <Value Type='Boolean'>1</Value>
                                               </Eq>
                                            </And>
                                            </Where>
                                        </Query>
                                      </View>";

                    query.ViewXml = string.Format(query.ViewXml, userId);
                    items = list.GetItems(query);
                    context.Load(items);
                    context.ExecuteQuery();

                    if (items != null && items.Count > 0)
                    {
                        foreach (ListItem item in items)
                        {
                            UserTrainingDetail training = new UserTrainingDetail();

                            var childIdFieldCourse = item["Skill"] as FieldLookupValue;
                            var childIdFieldModule = item["Training"] as FieldLookupValue;

                            if (childIdFieldCourse != null)
                            {
                                training.SkillName = childIdFieldCourse.LookupValue;
                                training.SkillId = childIdFieldCourse.LookupId;
                                if (childIdFieldModule != null)
                                {
                                    training.TrainingName = childIdFieldModule.LookupValue;
                                }
                                training.IsTrainingCompleted = false;
                                if (item["IsTrainingCompleted"] != null && item["IsTrainingCompleted"].ToString().ToUpper() == "TRUE")
                                {
                                    training.IsTrainingCompleted = true;
                                }
                                training.LastDayToComplete = Convert.ToDateTime(item["LastDayCompletion"]);
                                training.status = Utilities.GetTraningStatus(training.IsTrainingCompleted, training.NoOfAttempts, training.LastDayToComplete);
                                training.bgColor = Utilities.GetTrainingColor(training.status);
                                trainingsLst.Add(training);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetTrainingsList", ex.Message, ex.StackTrace));

            }
            return trainingsLst;
        }

        public List<UserSkillDetail> GetTrainingJourneyDetails(string SPlistName, int userId)
        {
            List<UserSkillDetail> userSkills = new List<UserSkillDetail>();

            //Populate all training modules associate with the current user
            List<UserTrainingDetail> userTrainings = GetSkillBasedTrainingsList(SPlistName, userId);
            ListItemCollection userAssessmentSPItems = GetUserAssessments(userId);
            List<UserAssessment> userAssessments = new List<UserAssessment>();

            if (userAssessmentSPItems != null && userAssessmentSPItems.Count > 0)
            {
                foreach (ListItem item in userAssessmentSPItems)
                {
                    UserAssessment userAssessment = new UserAssessment();
                    userAssessment.Id = ((FieldLookupValue)item["Assessment"]).LookupId;
                    userAssessment.SkillId = ((FieldLookupValue)item["Skill"]).LookupId;
                    if (item["IsAssessmentComplete"] != null)
                    {
                        userAssessment.IsAssessmentComplete = Convert.ToBoolean(item["IsAssessmentComplete"]);
                    }
                    userAssessments.Add(userAssessment);
                }
            }

            if (userTrainings != null && userTrainings.Count > 0)
            {
                userSkills = GetCouseWiseTrainingModules(userTrainings);

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
            using (ClientContext context = new ClientContext(CurrentSiteUrl))
            {
                context.Credentials = SPCredential;
                ListItemCollection lstTrainingMmodules = SharePointUtil.GetListItems(
                                    AppConstant.SkillCompetencyLevelTrainings, context, null, null);
                SkillCompetencies objTraining = null;
                foreach (ListItem item in lstTrainingMmodules)
                {
                    objTraining = new SkillCompetencies();
                    objTraining.SkillId = Convert.ToInt32(((FieldLookupValue)(item["Skill"])).LookupId);
                    objTraining.CompetenceId = Convert.ToInt32(((FieldLookupValue)(item["CompetencyLevel"])).LookupId);
                    objTraining.CompetenceName = Convert.ToString(((FieldLookupValue)(item["CompetencyLevel"])).LookupValue);
                    objTraining.TrainingName = Convert.ToString(item["Title"]);
                    objTraining.TrainingDescription = Convert.ToString(item["Description1"]);
                    string trainingLink = string.Empty;
                    if (Convert.ToString(item["TrainingCategory"]) == IngEnum.TrainingCategory.External.ToString() && String.IsNullOrEmpty(item.FieldValues["TrainingLink"].ToString()) == false)
                    {
                        FieldUrlValue trainingURL = (FieldUrlValue)item.FieldValues["TrainingLink"];
                        objTraining.TrainingLink = trainingURL != null ? trainingURL.Url : trainingLink;
                    }
                    objTraining.TrainingId = Convert.ToInt32(item.FieldValues["ID"]);
                    trainings.Add(objTraining);
                }
            }
            return trainings;
        }

       public ProjectResources GetExpectedProjectResourceCountByProjectId(ProjectResources prjRes)
        {           
            using (ClientContext ctx = new ClientContext(CurrentSiteUrl))
            {
                ctx.Credentials = SPCredential;

                CamlQuery querySkills = new CamlQuery();
                querySkills.ViewXml = @"<View>
                                         <Query>
                                            <Where>                                               
                                                  <Eq>
                                                     <FieldRef Name='Project' LookupId='True' />
                                                     <Value Type='Lookup'>{0}</Value>
                                                  </Eq>                                               
                                            </Where>
                                         </Query>
                                       </View>";
                querySkills.ViewXml = string.Format(querySkills.ViewXml, prjRes.projectId);
                List listResource = ctx.Web.Lists.GetByTitle(AppConstant.ProjectSkillResource);
                ListItemCollection listItemResource = listResource.GetItems(querySkills);
                ctx.Load(listItemResource);
                ctx.ExecuteQuery();


                if (listItemResource.Count > 0)
                {
                    for (int n = 0; n < listItemResource.Count; n++)
                    {
                        FieldLookupValue skill = listItemResource[n]["Skill"] as FieldLookupValue;
                        FieldLookupValue project = listItemResource[n]["Project"] as FieldLookupValue;
                        prjRes.projectName = project.LookupValue;

                        bool flag = false;
                        for (int j = 0; j < prjRes.skillResources.Count; j++)
                        {
                            if (prjRes.skillResources[j].skillId == skill.LookupId)
                                flag = true;
                        }

                        if (flag == false)
                        {
                            SkillResource s = new SkillResource();
                            s.skill = skill.LookupValue;
                            s.skillId = skill.LookupId;
                            prjRes.skillResources.Add(s);
                        }
                    }
                    for (int n = 0; n < listItemResource.Count; n++)
                    {
                        FieldLookupValue skill = listItemResource[n]["Skill"] as FieldLookupValue;
                        for (int j = 0; j < prjRes.skillResources.Count; j++)
                        {
                            if (prjRes.skillResources[j].skillId == skill.LookupId)
                            {
                                FieldLookupValue competencyLevel = listItemResource[n]["CompetencyLevel"] as FieldLookupValue;
                                if (competencyLevel.LookupValue.ToUpper() == "BEGINNER" || competencyLevel.LookupValue.ToUpper() == "NOVICE")       //Gets the count for Beginner/Novice resources.
                                {
                                    prjRes.skillResources[j].beginnerCount = Convert.ToInt32(listItemResource[n]["ExpectedResourceCount"].ToString());
                                }
                                else if (competencyLevel.LookupValue.ToUpper() == "ADVANCED BEGINNER")          //Gets the count for Advanced Beginner resources.
                                {
                                    prjRes.skillResources[j].advancedBeginnerCount = Convert.ToInt32(listItemResource[n]["ExpectedResourceCount"].ToString());
                                }
                                else if (competencyLevel.LookupValue.ToUpper() == "COMPETENT")      //Gets the count for Competent resources.
                                {
                                    prjRes.skillResources[j].competentCount = Convert.ToInt32(listItemResource[n]["ExpectedResourceCount"].ToString());
                                }
                                else if (competencyLevel.LookupValue.ToUpper() == "PROFICIENT")     //Gets the count for Proficient resources.
                                {
                                    prjRes.skillResources[j].proficientCount = Convert.ToInt32(listItemResource[n]["ExpectedResourceCount"].ToString());
                                }
                                else if (competencyLevel.LookupValue.ToUpper() == "EXPERT")     //Gets the count for Expert resources.
                                {
                                    prjRes.skillResources[j].expertCount = Convert.ToInt32(listItemResource[n]["ExpectedResourceCount"].ToString());
                                }
                            }
                        }
                    }
                }
            }
            return prjRes;
        }

        public void AddExpectedProjectResourceCountByProjectId(ProjectResources prjRes)
        {
            using (ClientContext ctx = new ClientContext(CurrentSiteUrl))
            {
                ctx.Credentials = SPCredential;
                for (int m = 0; m < prjRes.skillResources.Count; m++)
                {

                    CamlQuery queryResource = new CamlQuery();
                    queryResource.ViewXml = @"<View>
                                         <Query>
                                            <Where>
                                               <And>
                                                  <Eq>
                                                     <FieldRef Name='Project' LookupId='True' />
                                                     <Value Type='Lookup'>{0}</Value>
                                                  </Eq>
                                                  <Eq>
                                                     <FieldRef Name='Skill' LookupId='True' />
                                                     <Value Type='Lookup'>{1}</Value>
                                                  </Eq>
                                               </And>
                                            </Where>
                                         </Query>
                                       </View>";
                    queryResource.ViewXml = string.Format(queryResource.ViewXml, prjRes.projectId, prjRes.skillResources[m].skillId);
                    List listResource = ctx.Web.Lists.GetByTitle(AppConstant.ProjectSkillResource);
                    ListItemCollection listItemResource = listResource.GetItems(queryResource);
                    ctx.Load(listItemResource);
                    ctx.ExecuteQuery();

                    if (listItemResource.Count > 0)
                    {
                        for (int n = 0; n < listItemResource.Count; n++)
                        {
                            FieldLookupValue competencyLevel = listItemResource[n]["CompetencyLevel"] as FieldLookupValue;
                            if (competencyLevel.LookupValue.ToUpper() == "BEGINNER" || competencyLevel.LookupValue.ToUpper() == "NOVICE")
                            {
                                listItemResource[n]["ExpectedResourceCount"] = prjRes.skillResources[m].beginnerCount;
                            }
                            else if (competencyLevel.LookupValue.ToUpper() == "ADVANCED BEGINNER")
                            {
                                listItemResource[n]["ExpectedResourceCount"] = prjRes.skillResources[m].advancedBeginnerCount;
                            }
                            else if (competencyLevel.LookupValue.ToUpper() == "COMPETENT")
                            {
                                listItemResource[n]["ExpectedResourceCount"] = prjRes.skillResources[m].competentCount;
                            }
                            else if (competencyLevel.LookupValue.ToUpper() == "PROFICIENT")
                            {
                                listItemResource[n]["ExpectedResourceCount"] = prjRes.skillResources[m].proficientCount;
                            }
                            else if (competencyLevel.LookupValue.ToUpper() == "EXPERT")
                            {
                                listItemResource[n]["ExpectedResourceCount"] = prjRes.skillResources[m].expertCount;
                            }

                            listItemResource[n].Update();
                        }
                        ctx.ExecuteQuery();
                    }
                }
            }
        }
        
        #endregion



        #region ########## FUNCTIONALITY SPECIFIC PRIVATE METHODS ############

        private List<SkillCompetencies> GetCompetenciesBySkill(string name)
        {
            List<SkillCompetencies> competencies = new List<SkillCompetencies>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    string skillquery = @"<View>
                                        <Query>
                                            <Where>                                               
                                                <Eq>
                                                    <FieldRef Name='Title'  />
                                                    <Value Type='Text'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";

                    skillquery = string.Format(skillquery, name);

                    ListItemCollection lstSkill = SharePointUtil.GetListItems(
                        AppConstant.Skills, context, null, skillquery);

                    if (lstSkill != null & lstSkill.Count > 0)
                    {
                        int Id = lstSkill[0].Id;

                        if (Id > 0)
                        {
                            string query = @"<View>
                                        <Query>
                                            <Where>                                               
                                                <Eq>
                                                    <FieldRef Name='Skill' LookupId='True' />
                                                    <Value Type='Lookup'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                            query = string.Format(query, Id);

                            ListItemCollection lstSkillCompetency = SharePointUtil.GetListItems(
                                AppConstant.SkillCompetencyLevels, context, null, query);

                            if (lstSkillCompetency != null & lstSkillCompetency.Count > 0)
                            {
                                SkillCompetencies objCompetencyDetail = null;
                                foreach (ListItem item in lstSkillCompetency)
                                {
                                    objCompetencyDetail = new SkillCompetencies();

                                    string queryTraining = @"<View>
                                        <Query>
                                            <Where>
                                                <And>                                               
                                                    <Eq>
                                                        <FieldRef Name='Skill' LookupId='True' />
                                                        <Value Type='Lookup'>{0}</Value>
                                                    </Eq>
                                                    <Eq>
                                                        <FieldRef Name='CompetencyLevel' LookupId='True' />
                                                        <Value Type='Lookup'>{1}</Value>
                                                    </Eq>
                                                </And>
                                            </Where>
                                        </Query>
                                      </View>";
                                    queryTraining = string.Format(queryTraining, ((FieldLookupValue)(
                                        item["Skill"])).LookupId, item.Id);

                                    ListItemCollection lstTrainingMmodules = SharePointUtil.GetListItems(
                                        AppConstant.SkillCompetencyLevelTrainings, context, null, queryTraining);

                                    var ulHTML = new TagBuilder("ul");
                                    StringBuilder output = new StringBuilder();
                                    string trainingDescription = "";
                                    foreach (ListItem lstitem in lstTrainingMmodules)
                                    {

                                        var liHTML = new TagBuilder("li");
                                        liHTML.MergeAttribute("style", "list-style-type:disc;margin-left:15px");
                                        liHTML.SetInnerText(lstitem["Title"].ToString());
                                        output.Append(liHTML.ToString());
                                        trainingDescription += lstitem["Description1"] + System.Environment.NewLine;
                                    }
                                    ulHTML.InnerHtml = output.ToString();
                                    objCompetencyDetail.CompetenceId = item.Id;
                                    objCompetencyDetail.CompetenceName = Convert.ToString(item["Title"]);
                                    objCompetencyDetail.Description = Convert.ToString(item["Description1"]);
                                    objCompetencyDetail.SkillId = ((FieldLookupValue)(item["Skill"])).LookupId;
                                    objCompetencyDetail.TrainingName = ulHTML.ToString();
                                    objCompetencyDetail.TrainingDescription = trainingDescription;
                                    competencies.Add(objCompetencyDetail);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetCompetenciesBySkill", ex.Message, ex.StackTrace));

            }
            return competencies;
        }

        private List<SkillCompetencies> GetCompetenciesBySkillForOnboardedUser(int Id, List<Competence> allCompetencies)
        {
            List<SkillCompetencies> competencies = new List<SkillCompetencies>();
            if (Id > 0)
            {
                List<Competence> lstSkillCompetency = allCompetencies.Where(s => s.SkillId == Id).ToList();
                if (lstSkillCompetency != null & lstSkillCompetency.Count > 0)
                {
                    SkillCompetencies objCompetencyDetail = null;
                    foreach (Competence item in lstSkillCompetency)
                    {
                        objCompetencyDetail = new SkillCompetencies();
                        objCompetencyDetail.CompetenceId = item.CompetenceId;
                        objCompetencyDetail.CompetenceName = item.CompetenceName;
                        //objCompetencyDetail.Description = item.des
                        objCompetencyDetail.SkillId = item.SkillId;
                        competencies.Add(objCompetencyDetail);
                    }
                }
            }
            return competencies;
        }
        private List<SiteMenu> GetMenuList(ListItemCollection itemsMenu)
        {
            List<SiteMenu> siteMenu = null;


            if (HttpContext.Current.Session["UserSiteMenu"] == null)
            {
                siteMenu = new List<SiteMenu>();
                foreach (ListItem item in itemsMenu)
                {
                    SiteMenu menu = new SiteMenu();
                    menu.ItemId = int.Parse(item["ID"].ToString());
                    menu.ItemName = item["Title"].ToString();
                    menu.ParentItemId = int.Parse(item["ParentMenu"].ToString());
                    menu.ItemOrdering = int.Parse(item["Ordering"].ToString());
                    if (item["ControllerView"] != null)
                    {
                        menu.ItemURL = Convert.ToString(item["ControllerView"]);
                    }
                    else
                    {
                        menu.ItemURL = ((FieldUrlValue)(item["URL1"])).Url; //(item["URL"].ToString());
                    }
                    menu.ItemTarget = (item["Target"].ToString());
                    menu.ItemHidden = (item["Hidden"].ToString());

                    var userRolesLookUp = item["Roles"] as FieldLookupValue[];

                    if (userRolesLookUp != null)
                    {
                        menu.UserRole = new List<UserRole>();
                        foreach (var userRole in userRolesLookUp)
                        {
                            UserRole objUserRole = new UserRole();
                            objUserRole.RoleId = userRole.LookupId;
                            objUserRole.RoleName = userRole.LookupValue;
                            menu.UserRole.Add(objUserRole);

                        }
                    }

                    if (menu.ItemName == "Admin")
                    {
                        string spPMOGroup = ConfigurationManager.AppSettings["AcademyPMO"].ToString();
                        if (CurrentUser.Groups.Exists(t => t == spPMOGroup))
                        {
                            siteMenu.Add(menu);
                        }
                    }
                    else
                    {
                        siteMenu.Add(menu);

                    }

                }
            }

            return siteMenu;
        }


        ////Get Manager Details
        private string GetManagerDetails(PersonProperties personProperties)
        {
            string managerName = string.Empty;

            if (personProperties.ExtendedManagers != null && personProperties.ExtendedManagers.Count() > 0)
            {
                try
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;

                        PeopleManager myManager = new PeopleManager(context);
                        PersonProperties myManagerpersonProperties = myManager.GetPropertiesFor(
                            personProperties.ExtendedManagers.Last());
                        context.Load(myManagerpersonProperties);
                        context.ExecuteQuery();
                        managerName = myManagerpersonProperties.DisplayName;
                    }
                }
                catch (Exception ex)
                {
                    UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetManagerDetails", ex.Message, ex.StackTrace));

                }
            }

            return managerName;
        }

        ////Get Peers Details
        private List<string> GetPeersDetails(PersonProperties personProperties)
        {
            List<string> peersname = new List<string>();

            if (personProperties.Peers != null && personProperties.Peers.Count() > 0)
            {
                try
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;

                        foreach (string peers in personProperties.Peers)//.Take(5))
                        {
                            string peer = peers;
                            PeopleManager peersDetails = new PeopleManager(context);
                            PersonProperties peerDetailsProperties = peersDetails.GetPropertiesFor(peer);
                            context.Load(peerDetailsProperties);
                            context.ExecuteQuery();
                            peersname.Add(peerDetailsProperties.DisplayName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetPeersDetails", ex.Message, ex.StackTrace));

                }
            }

            return peersname;
        }

        ////Get Reportee Details
        private List<string> GetReporteeDetails(PersonProperties personProperties)
        {
            List<string> reprteename = new List<string>();

            if (personProperties.DirectReports != null && personProperties.DirectReports.Count() > 0)
            {
                try
                {
                    using (ClientContext context = new ClientContext(CurrentSiteUrl))
                    {
                        context.Credentials = SPCredential;

                        foreach (string reportee in personProperties.DirectReports)
                        {
                            string reporteeemail = reportee;
                            PeopleManager reporteeDetails = new PeopleManager(context);
                            PersonProperties reporteeDetailsProperties = reporteeDetails.GetPropertiesFor(
                                reporteeemail);
                            context.Load(reporteeDetailsProperties);
                            context.ExecuteQuery();
                            reprteename.Add(reporteeDetailsProperties.DisplayName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                    LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetReporteeDetails", ex.Message, ex.StackTrace));

                }
            }

            return reprteename;
        }

        private List<Competence> GetCompetenciesForASkill(string skillid)
        {
            List<Competence> competencies = new List<Competence>();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    string query = @"<View>
				                        <Query>
					                        <Where>                                               
						                        <Eq>
							                        <FieldRef Name='Skill' LookupId='True' />
							                        <Value Type='Lookup'>{0}</Value>
						                        </Eq>
					                        </Where>
				                        </Query>
			                        </View>";
                    query = string.Format(query, Convert.ToInt32(skillid));

                    ListItemCollection lstSkillCompetency = SharePointUtil.GetListItems(
                        AppConstant.SkillCompetencyLevels, context, null, query);

                    if (lstSkillCompetency != null & lstSkillCompetency.Count > 0)
                    {
                        Competence competance = null;
                        foreach (ListItem item in lstSkillCompetency)
                        {
                            competance = new Competence()
                            {
                                CompetenceId = item.Id,
                                CompetenceName = Convert.ToString(item["Title"])
                            };
                            competencies.Add(competance);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetCompetenciesForASkill", ex.Message, ex.StackTrace));

            }
            return competencies;
        }

        //Recursive Approach to create child objects for Wiki docs
        private List<WikiDocuments> GetChild(List<WikiDocuments> wikiDoc)
        {
            //Get child items
            List<WikiDocuments> wikiDocchild = new List<WikiDocuments>();
            foreach (WikiDocuments item in wikiDoc)
            {
                var wikichilddoc = from c in wikiDoc where c.DocumentURL.Equals(item.ParentFolderURL) select c;
                foreach (WikiDocuments itemwiki in wikichilddoc.ToList())
                {
                    if (itemwiki.WikiChild == null)
                    {
                        itemwiki.WikiChild = new List<WikiDocuments>();
                    }

                    itemwiki.WikiChild.Add(item);
                }

            }
            var d = from c in wikiDoc where c.ParentFolder.Equals("TrainingDocuments") select c;
            return d.ToList();
        }

        private List<Training> GetTrainingForSkillAndCompetency(int skillid, int competencyid)
        {
            List<Training> trainings = GetTrainings(skillid, competencyid);
            return trainings;
        }

        private List<UserDetails> GetUserDetails(ClientContext ctx, int skillId, int competenceId)
        {
            List<Training> trainings = new List<Training>();
            List<Training> lstAllTrainings = new List<Training>();
            List<UserDetails> userDetails = new List<UserDetails>();
            List<UserDetails> userTrainings = new List<UserDetails>();
            List<UserDetails> allDetails = new List<UserDetails>();
            string query = "";
            if (competenceId == 0)
            {
                query = @"<View>
                        <Query>
                            <Where>
                                    <Eq>
                                       <FieldRef Name='Skill'  LookupId='True' />
                                       <Value Type='Lookup'>{0}</Value>
                                    </Eq>                                                                       
                            </Where>
                       </Query>
                        <ViewFields>
                        <FieldRef Name='Title' />
                        <FieldRef Name='IsMandatory' />
                        <FieldRef Name='Skill' />
                        <FieldRef Name='CompetencyLevel' />
                        </ViewFields>
                        <QueryOptions />
                    </View>";
                query = string.Format(query, skillId);
            }

            else
            {
                query = @"<View>
                        <Query>
                            <Where>
                                   <And>
                                        <Eq>
                                            <FieldRef Name='Skill'  LookupId='True' />
                                                <Value Type='Lookup'>{0}</Value>
                                        </Eq>
                                        <Eq>
                                            <FieldRef Name='CompetencyLevel'  LookupId='True'/>
                                                <Value Type='Lookup'>{1}</Value>
                                        </Eq>
                                   </And>
                            </Where>
                       </Query>
                        <ViewFields>
                        <FieldRef Name='Title' />
                        <FieldRef Name='IsMandatory' />
                        <FieldRef Name='Skill' />
                        <FieldRef Name='CompetencyLevel' />
                        </ViewFields>
                        <QueryOptions />
                    </View>";
                query = string.Format(query, skillId, competenceId);
            }

            ListItemCollection allTrainings = SharePointUtil.GetListItems(AppConstant.SkillCompetencyLevelTrainings, ctx, null, query); //Fetching the data in the Skill Competency Level Training List
            if (allTrainings != null & allTrainings.Count > 0)
            {
                UserDetails allskill = null;
                foreach (ListItem item in allTrainings)
                {
                    allskill = new UserDetails()
                    {
                        TrainingId = item.Id,
                        TrainingName = Convert.ToString(item["Title"]),
                        IsMandatory = Convert.ToBoolean(item["IsMandatory"]),
                        skillName = Convert.ToString(item["Skill"]),
                        competenceName = item["CompetencyLevel"] != null ? ((FieldLookupValue)(item["CompetencyLevel"])).LookupValue : ""
                    };
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
                    lstAllUsers = GetUsersForAllTrainings(ctx);             //Fetching user details corresponding to the Trainings
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

            return allDetails;
        }


        private List<UserDetails> GetUsersForAllTrainings(ClientContext context)
        {
            List<UserDetails> lstUsersDetails = new List<UserDetails>();

            try
            {
                using (context)
                {
                    ListItemCollection collection = SharePointUtil.GetListItems(AppConstant.UserTrainingMapping, context, null, null);
                    context.Load(collection);
                    context.ExecuteQuery();
                    UserDetails objTraining = null;
                    if (collection != null && collection.Count > 0)
                    {
                        foreach (var item in collection)
                        {
                            objTraining = new UserDetails();
                            objTraining.TrainingId = Convert.ToInt32(item["ID"]);
                            objTraining.IsTrainingCompleted = Convert.ToBoolean(item["IsTrainingCompleted"]);
                            objTraining.TrainingCourse = item["Training"] != null ? ((FieldLookupValue)(item["Training"])).LookupValue : "";
                            objTraining.Employee = item["User1"] != null ? ((FieldLookupValue)(item["User1"])).LookupValue : "";
                            objTraining.skillName = item["Skill"].ToString();
                            lstUsersDetails.Add(objTraining);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUsersForAllTrainings", ex.Message, ex.StackTrace));

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

        private List<OnBoarding> GetAssignedTrainings(ClientContext context, UserManager user, List<OnBoarding> boardingList, ArrayList defaultTrainingIds, string showDefaultTraining, string showSkillBasedTraining)
        {
            try
            {
                List skillBasedTrainingList = context.Web.Lists.GetByTitle(AppConstant.UserTrainingMapping);
                CamlQuery q = new CamlQuery();

                q.ViewXml = @"<View>
                                    <Query>
                                        <Where>                                           
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>                                                                                          
                                        </Where>
                                    </Query>
                            </View>";
                q.ViewXml = string.Format(q.ViewXml, user.SPUserId, 1);
                ListItemCollection skillBasedTrainingCollection = skillBasedTrainingList.GetItems(q);
                context.Load(skillBasedTrainingCollection);
                context.ExecuteQuery();

                if (skillBasedTrainingCollection != null && skillBasedTrainingCollection.Count() > 0)
                {
                    List<SkillCompetencies> trainingList = GetSkillCompetencyTraingings();
                    foreach (ListItem skillBasedTrainingItem in skillBasedTrainingCollection)
                    {
                        if ((FieldLookupValue)skillBasedTrainingItem["Training"] != null)
                        {
                            FieldLookupValue traininglookupValue = (FieldLookupValue)skillBasedTrainingItem["Training"];
                            if ((defaultTrainingIds.Contains(traininglookupValue.LookupId) && showDefaultTraining == "YES") || (!defaultTrainingIds.Contains(traininglookupValue.LookupId) && showSkillBasedTraining == "YES"))
                            {
                                List<SkillCompetencies> selectedTraining = trainingList.Where(t => t.TrainingId == traininglookupValue.LookupId).ToList();
                                SkillCompetencies trainingItem = selectedTraining != null && selectedTraining.Count() == 1 ? selectedTraining.SingleOrDefault() : null;
                                bool trainingStatus = false;
                                if (skillBasedTrainingItem.FieldValues["IsTrainingCompleted"] != null && Convert.ToBoolean(skillBasedTrainingItem["IsTrainingCompleted"]) == true)
                                {
                                    trainingStatus = true;
                                }
                                if (trainingItem != null)
                                {
                                    HCLAcademy.Models.TraningStatus status = Utilities.GetTraningStatus(trainingStatus, 0, Convert.ToDateTime(skillBasedTrainingItem["LastDayCompletion"]));
                                    ///Traning Status Started                           
                                    boardingList.Add(new OnBoarding
                                    {
                                        BoardingItemId = Convert.ToInt32(skillBasedTrainingItem.FieldValues["ID"]),
                                        BoardingItemName = Convert.ToString(trainingItem.TrainingName),
                                        BoardingItemDesc = Convert.ToString(trainingItem.Description),
                                        BoardingItemLink = trainingItem.TrainingLink,
                                        BoardingStatus = Utilities.GetOnBoardingStatus(trainingStatus, 0, Convert.ToDateTime(skillBasedTrainingItem["LastDayCompletion"])),
                                        BoardingType = OnboardingItemType.Training,
                                        BoardIngTrainingId = trainingItem.TrainingId,
                                        BoardingIsMandatory = Convert.ToBoolean(skillBasedTrainingItem.FieldValues["IsMandatory"])
                                    });
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, UploadProfile", ex.Message, ex.StackTrace));

                Utilities.LogToEventVwr(ex.Message, 0);

            }
            return boardingList;
        }

        private OnBoardingColumns GetOnBoardingCoulms()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(OnBoardingColumns));
            OnBoardingColumns result = new OnBoardingColumns();

            try
            {
                using (ClientContext context = new ClientContext(CurrentSiteUrl))
                {
                    context.Credentials = SPCredential;

                    string filePath = Convert.ToString(ConfigurationManager.AppSettings["URL"]) + "/" + "Lists/" +
                        AppConstant.AcademyOnBoardingColumnSchemaInternalName + "/" + "OnBoardingColumns.xml";

                    using (Stream fileStream = SharePointUtil.GetFile(filePath, context))
                    {
                        result = (OnBoardingColumns)serializer.Deserialize(fileStream);
                    }
                }
            }
            catch (FileLoadException ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetUserMandatoryTrainings", ex.Message, ex.StackTrace));


            }
            return result;
        }

        private List<CheckListItem> GetOnBoardingCheckList(string locValue, int locId)
        {
            List<CheckListItem> checklistItems = GetAllChecklist();
            List<CheckListItem> filteredItems = checklistItems.Where(c => c.GEOId == locId).ToList();
            return filteredItems;
        }

        private OnboardingStatus GetStatus(object value)
        {
            if (Convert.ToString(value).ToLower().Trim() == "completed" || Convert.ToString(value).ToLower().Trim() == "true")
            {
                return OnboardingStatus.Completed;
            }

            if (Convert.ToString(value).ToLower().Trim() == "initiated")
            {
                return OnboardingStatus.OnGoing;
            }

            if (Convert.ToString(value).ToLower().Trim() == "rejected" || Convert.ToString(value).ToLower().Trim() == "false")
            {
                return OnboardingStatus.Rejected;
            }

            return OnboardingStatus.NotStarted;
        }

        private List<OnBoarding> GetUserAssessments(ClientContext context, List<OnBoarding> boardingList)
        {
            List<int> ids = new List<int>();
            List userAssessmentList = context.Web.Lists.GetByTitle(AppConstant.UserAssessmentMapping);
            CamlQuery q = new CamlQuery();
            q.ViewXml = @"<View>
                                    <Query>
                                        <Where>
                                            <And>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                                <Eq>
                                                     <FieldRef Name='IsIncludeOnBoarding' />
                                                     <Value Type='Boolean'>{1}</Value>
                                                </Eq>
                                            </And>
                                        </Where>
                                    </Query>
                            </View>";
            q.ViewXml = string.Format(q.ViewXml, CurrentUser.SPUserId, 1);
            ListItemCollection userAssessments = userAssessmentList.GetItems(q);
            context.Load(userAssessments);
            context.ExecuteQuery();

            if (userAssessments != null && userAssessments.Count() > 0)
            {
                foreach (ListItem usrAssmtItem in userAssessments)
                {
                    FieldLookupValue assessmentlookupValue = (FieldLookupValue)usrAssmtItem["Assessment"];
                    List<Assessment> allAssessments = GetAllAssessments();
                    Assessment assessmentItem = allAssessments.Where(assmt => assmt.AssessmentId == ((FieldLookupValue)(usrAssmtItem["Assessment"])).LookupId).ToList().FirstOrDefault();
                    bool assessmentStatus = false;
                    if (usrAssmtItem.FieldValues["IsAssessmentComplete"] != null && Convert.ToBoolean(usrAssmtItem["IsAssessmentComplete"]) == true)
                    {
                        assessmentStatus = true;
                    }
                    if (assessmentItem != null)
                    {
                        OnBoarding board = new OnBoarding();
                        board.BoardingItemId = Convert.ToInt32(usrAssmtItem.FieldValues["ID"]);
                        board.BoardingItemName = assessmentItem.AssessmentName;
                        board.BoardingItemDesc = assessmentItem.Description;
                        board.BoardingStatus = (OnboardingStatus)Utilities.GetOnBoardingStatus
                                                (
                                                    assessmentStatus,
                                                    Convert.ToInt32(usrAssmtItem["NoOfAttempt"]),
                                                    Convert.ToDateTime(usrAssmtItem["LastDayCompletion"])
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
            return boardingList;
        }

        private bool ProfileSharingEmail(ClientContext context, int onboardID, string ModifiedDt)
        {
            bool result = false;
            try
            {
                UserManager user = CurrentUser;
                string to = string.Empty;
                string link = string.Empty;
                link = context.Web.Url.ToString() + "Lists/AcademyOnBoarding/EditForm.aspx?ID=" + Convert.ToString(onboardID);

                Hashtable hashtable = new Hashtable();

                hashtable.Add("UserName", user.UserName);
                hashtable.Add("ClientName", ConfigurationManager.AppSettings["ClientName"].ToString());
                hashtable.Add("Link", link);
                hashtable.Add("ModifiedDate", ModifiedDt);

                string spOwnerGroup = ConfigurationManager.AppSettings["AcademyPMO"].ToString();
                Microsoft.SharePoint.Client.Group SpGroups = context.Web.SiteGroups.GetByName(spOwnerGroup);
                context.Load(SpGroups, grp => grp.Users);
                context.ExecuteQuery();

                foreach (User Usr in SpGroups.Users)
                {
                    to += Usr.Email + ";";
                }

                if (to != null)
                {
                    bool Queue = SharePointUtil.AddToEmailQueue(context, "ProfileSharingWorkflow", hashtable, to, null);

                    #region update fields
                    List listOnBoard = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                    ListItemCreationInformation itemCreateInfoOnBoard = new ListItemCreationInformation();

                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                      </View>";
                    query.ViewXml = string.Format(query.ViewXml, user.SPUserId);

                    ListItemCollection listItemOnBoard = listOnBoard.GetItems(query);
                    context.Load(listItemOnBoard);
                    context.ExecuteQuery();
                    ListItem itemOnBoard = listItemOnBoard.SingleOrDefault();

                    itemOnBoard["SendEmail"] = true;
                    itemOnBoard["ProfileSharingWorkflow"] = context.Web.Url.ToString() + ", Completed: Mail Sent & List Item Updated";// Workflow column being updated

                    itemOnBoard.Update();
                    context.ExecuteQuery();
                    #endregion
                }

                result = true;
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, ProfileSharingEmail", ex.Message, ex.StackTrace));
                Utilities.LogToEventVwr(ex.Message, 0);
            }

            return result;
        }

        private string StripHTML(string html)
        {
            var regex = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
            return System.Web.HttpUtility.HtmlDecode((regex.Replace(html, "")));
        }

        private static ListItemCollection GetTrainingTree(string lstGetName, ClientContext context, string webName, string camlQuery)
        {
            ListItemCollection itemsColl = null;
            try
            {
                CamlQuery query = new CamlQuery();

                if (!string.IsNullOrEmpty(webName))
                {

                    Microsoft.SharePoint.Client.Web web = context.Site.OpenWeb(webName);
                    context.Load(web, w => w.Lists);
                    List lst = web.Lists.GetByTitle(lstGetName);
                    context.Load(lst);
                    context.ExecuteQuery();
                    if (string.IsNullOrEmpty(camlQuery))
                    {
                        query = CamlQuery.CreateAllItemsQuery();
                    }
                    else
                    {
                        query.ViewXml = camlQuery;
                    }


                    itemsColl = lst.GetItems(query);

                    // Retrieve all items in the ListItemCollection from List.GetItems(Query).

                    context.Load(itemsColl, items => items.Include(item => item.Id, item => item.EffectiveBasePermissions, item => item.DisplayName, item => item.Folder, item => item.FileSystemObjectType, item => item.Folder.ParentFolder.Name, item => item["EncodedAbsUrl"], item => item["FileRef"], item => item["ContentBody"], item => item["ContentUrl"], item => item["Title"], item => item["OrderBy"]));

                    context.ExecuteQuery();
                    return itemsColl;
                }
                else
                {
                    Microsoft.SharePoint.Client.Web web = context.Web;
                    context.Load(web, w => w.Lists);
                    List lst = web.Lists.GetByTitle(lstGetName);
                    context.Load(lst);
                    context.ExecuteQuery();
                    if (string.IsNullOrEmpty(camlQuery))
                    {
                        query = CamlQuery.CreateAllItemsQuery();
                    }
                    else
                    {
                        query.ViewXml = camlQuery;
                    }
                    itemsColl = lst.GetItems(query);

                    // Retrieve all items in the ListItemCollection from List.GetItems(Query). 

                    context.Load(itemsColl, items => items.Include(item => item.Id, item => item.EffectiveBasePermissions, item => item.DisplayName, item => item.Folder, item => item.FileSystemObjectType, item => item.Folder.ParentFolder.Name, item => item["EncodedAbsUrl"], item => item["FileRef"], item => item["ContentBody"], item => item["ContentUrl"], item => item["Title"], item => item["OrderBy"]));
                    context.ExecuteQuery();


                }

            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetTrainingTree", ex.Message, ex.StackTrace));


            }
            return itemsColl;

        }

        private static bool AddAttachmentToOnBoardingList(byte[] buffer, ClientContext context, string fileName, out string fileRelativeURL)
        {
            fileRelativeURL = string.Empty;
            bool status = false;
            UserManager user = (UserManager)System.Web.HttpContext.Current.Session["CurrentUser"];

            try
            {
                using (context)
                {
                    List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
                    context.Load(academyOnBoarding, acd => acd.WorkflowAssociations);
                    CamlQuery query = new CamlQuery();
                    query.ViewXml = @"<View>
                                        <Query>
                                            <Where>
                                                <Eq>
                                                    <FieldRef Name='User1' LookupId='True' />
                                                    <Value Type='User'>{0}</Value>
                                                </Eq>
                                            </Where>
                                        </Query>
                                   </View>";
                    query.ViewXml = string.Format(query.ViewXml, user.SPUserId);

                    ListItemCollection collection = academyOnBoarding.GetItems(query);
                    context.Load(collection);
                    context.ExecuteQuery();
                    if (collection != null && collection.Count > 0)
                    {
                        ListItem item = collection.SingleOrDefault();
                        AttachmentCollection files = item.AttachmentFiles;
                        context.Load(files);
                        context.ExecuteQuery();
                        if (files != null & files.Count > 0)
                        {
                            DeleteAttachment(item.Id, context);
                        }
                        ListItem newitemR = collection.GetById(item.Id);
                        context.Load(newitemR);
                        context.ExecuteQuery();
                        newitemR.RefreshLoad();
                        newitemR["ProfileSharing"] = "Initiated";
                        newitemR.Update();
                        context.ExecuteQuery();
                        ListItem newitem = collection.GetById(item.Id);
                        newitem.RefreshLoad();
                        var attInfo = new AttachmentCreationInformation();
                        attInfo.FileName = fileName;
                        attInfo.ContentStream = new MemoryStream(buffer);
                        Attachment att = newitem.AttachmentFiles.Add(attInfo); //Add to File
                        context.Load(att);
                        context.ExecuteQuery();
                        fileRelativeURL = att.ServerRelativeUrl;
                        status = true;
                    }

                }
            }
            catch (Exception ex)
            {
                UserManager loggeduser = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, loggeduser.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, AddAttachmentToOnBoardingList", ex.Message, ex.StackTrace));

            }
            return status;
        }

        private static void DeleteAttachment(int id, ClientContext context)
        {
            List academyOnBoarding = context.Web.Lists.GetByTitle(AppConstant.AcademyOnBoarding);
            ListItem item = academyOnBoarding.GetItemById(id);
            context.Load(item, i => i.AttachmentFiles);
            context.ExecuteQuery();
            item.AttachmentFiles.ToList().ForEach(a => a.DeleteObject());
            context.Load(item);
            context.ExecuteQuery();
            ListItem itemD = academyOnBoarding.GetItemById(id);
            item.RefreshLoad();
            itemD["ProfileSharing"] = "Rejected";
            item.Update();
            context.ExecuteQuery();
        }


        #endregion



        #region ########## GENERIC PRIVATE METHODS ############
        private ListItemCollection GetPolicyDocumentItems(string lstGetName, ClientContext context, string webName, string camlQuery)
        {
            ListItemCollection itemsColl = null;
            try
            {
                CamlQuery query = new CamlQuery();

                if (!string.IsNullOrEmpty(webName))
                {
                    Microsoft.SharePoint.Client.Web web = context.Site.OpenWeb(webName);
                    context.Load(web, w => w.Lists);
                    List lst = web.Lists.GetByTitle(lstGetName);
                    context.Load(lst);
                    context.ExecuteQuery();
                    if (string.IsNullOrEmpty(camlQuery))
                    {
                        query = CamlQuery.CreateAllItemsQuery();
                    }
                    else
                    {
                        query.ViewXml = camlQuery;
                    }

                    itemsColl = lst.GetItems(query);

                    // Retrieve all items in the ListItemCollection from List.GetItems(Query).
                    context.Load(itemsColl, items => items.Include(item => item.Id, item => item.DisplayName, item => item["EncodedAbsUrl"], item => item["PolicyOwner"]));
                    context.ExecuteQuery();
                    return itemsColl;
                }
                else
                {
                    Microsoft.SharePoint.Client.Web web = context.Web;
                    context.Load(web, w => w.Lists);
                    List lst = web.Lists.GetByTitle(lstGetName);
                    context.Load(lst);
                    context.ExecuteQuery();
                    if (string.IsNullOrEmpty(camlQuery))
                    {
                        query = CamlQuery.CreateAllItemsQuery();
                    }
                    else
                    {
                        query.ViewXml = camlQuery;
                    }
                    itemsColl = lst.GetItems(query);

                    // Retrieve all items in the ListItemCollection from List.GetItems(Query). 
                    context.Load(itemsColl, items => items.Include(item => item.Id, item => item.DisplayName, item => item["EncodedAbsUrl"], item => item["PolicyOwner"]));
                    context.ExecuteQuery();

                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetPolicyDocumentItems", ex.Message, ex.StackTrace));


            }
            return itemsColl;
        }

        private ListItemCollection GetWikiDocumentItems(string lstGetName, ClientContext context, string webName, string camlQuery)
        {
            ListItemCollection itemsColl = null;
            try
            {
                CamlQuery query = new CamlQuery();

                if (!string.IsNullOrEmpty(webName))
                {
                    Microsoft.SharePoint.Client.Web web = context.Site.OpenWeb(webName);
                    context.Load(web, w => w.Lists);
                    List lst = web.Lists.GetByTitle(lstGetName);
                    context.Load(lst);
                    context.ExecuteQuery();
                    if (string.IsNullOrEmpty(camlQuery))
                    {
                        query = CamlQuery.CreateAllItemsQuery();
                    }
                    else
                    {
                        query.ViewXml = camlQuery;
                    }

                    itemsColl = lst.GetItems(query);

                    // Retrieve all items in the ListItemCollection from List.GetItems(Query).

                    context.Load(itemsColl, items => items.Include(item => item.Id, item => item.EffectiveBasePermissions, item => item.DisplayName, item => item.Folder, item => item.FileSystemObjectType, item => item.Folder.ParentFolder.Name, item => item["EncodedAbsUrl"], item => item["FileRef"]));
                    context.ExecuteQuery();
                    return itemsColl;
                }
                else
                {
                    Microsoft.SharePoint.Client.Web web = context.Web;
                    context.Load(web, w => w.Lists);
                    List lst = web.Lists.GetByTitle(lstGetName);
                    context.Load(lst);
                    context.ExecuteQuery();
                    if (string.IsNullOrEmpty(camlQuery))
                    {
                        query = CamlQuery.CreateAllItemsQuery();
                    }
                    else
                    {
                        query.ViewXml = camlQuery;
                    }
                    itemsColl = lst.GetItems(query);

                    // Retrieve all items in the ListItemCollection from List.GetItems(Query). 
                    context.Load(itemsColl, items => items.Include(item => item.Id, item => item.EffectiveBasePermissions, item => item.DisplayName, item => item.Folder, item => item.FileSystemObjectType, item => item.Folder.ParentFolder.Name, item => item["EncodedAbsUrl"], item => item["FileRef"]));
                    context.ExecuteQuery();

                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetWikiDocumentItems", ex.Message, ex.StackTrace));


            }
            return itemsColl;
        }

        private static ListItemCollection GetFolderItems(string ListName, string folder)
        {
            ListItemCollection items = null;
            try
            {
                string url = ConfigurationManager.AppSettings["URL"].ToString();
                using (ClientContext context = new ClientContext(url))
                {
                    context.Credentials = (ICredentials)HttpContext.Current.Session["SPCredential"];
                    Microsoft.SharePoint.Client.Web web = context.Web;
                    context.Load(web, w => w.ServerRelativeUrl);
                    List lst = web.Lists.GetByTitle(ListName);
                    context.ExecuteQuery();
                    context.Load(lst, l => l.Fields);

                    var query1 = new CamlQuery();

                    query1.FolderServerRelativeUrl = web.ServerRelativeUrl + folder;

                    query1.ViewXml = "<View Scope=\"RecursiveAll\"> " +
                        "<Query>" +
                        "<Where>" +
                                    "<Contains>" +
                                        "<FieldRef Name=\"FileDirRef\" />" +
                                        "<Value Type=\"Text\">" + folder + "</Value>" +
                                     "</Contains>" +
                        "</Where>" +
                        "</Query>" +
                        "</View>";

                    items = lst.GetItems(query1);

                    // Retrieve all items in the ListItemCollection from List.GetItems(Query). 
                    context.Load(items);
                    context.ExecuteQuery();

                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "SharePointDAL, GetFolderItems", ex.Message, ex.StackTrace));


            }
            return items;
        }


        #endregion


    }
}