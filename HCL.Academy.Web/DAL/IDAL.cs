using HCLAcademy.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace HCL.Academy.Web.DAL
{
    public interface IDAL
    {
        void CacheConfig();

        List<AcademyVideo> GetAllAcademyVideos();

        FileStreamResult GetVideoStream(string url);

        Assessments GetAssessmentDetails(int AssessmentId);

        bool AssessmentResult(AssesmentResult result, List<QuestionDetail> QuestionDetails);

        List<Project> GetAllProjects();

        List<Users> GetUsers();

        void UpdateProjectData(AssignUser objUserOnboard);

        List<WikiPolicies> GetAllWikiPolicies();

        void AddProject(string projectName);

        void UpdateProject(Project project);

        void RemoveProject(int projectID);

        Project EditProjectByID(int projectID);

        List<Skills> GetSkills();

        List<Result> Search(string keyword);

        HeatMapProjectDetail GetHeatMapProjectDetailByProjectID(int projectID);

        List<SiteMenu> GetMenu();

        UserManager GetCurrentUserCompleteUserProfile();

        List<Banners> GetBanners();

        Resource GetResourceDetailsByProjectID(int projectID);

        List<Skill> GetAllSkills();

        ProjectDetails GetProjectSkillsByProjectID(string projectID);

        ProjectSkill PostProjectSkill(string projectid, string skillid);

        void DeleteProjectSkill(int projectskillid, string projectid, string skillid);

        TrainingReport GetTrainingsReport(string skillid, string competencyid);

        List<RSSFeed> GetRSSFeeds();

        List<News> GetNews(string NoImagePath);

        List<AcademyEvent> GetEvents();

        List<TrainingPlan> GetTrainingPlans(int id);

        List<UserTraining> GetTrainingForUser(int userId,bool OnlyOnBoardedTraining = false);

        List<OnBoarding> GetBoardingData();

        List<UserSkill> GetUserSkillsOfCurrentUser();

        List<Competence> GetAllCompetenceList();

        List<ProjectSkill> GetAllProjectSkills();

        void AddProjectSkill(int ProjectID, int SkillID);

        List<ProjectSkillResource>  GetAllProjectSkillResources();

        void AddProjectSkillResource(int ProjectID, ProjectSkillResource skillResource);

        void AddProjectSkillResources(ProjectResources prjRes);

        List<ProjectSkillResource> GetAllProjectSkillResourcesByProjectID(int ProjectID);

        List<OnBoarding> GetBoardingDataFromOnboarding(ref bool sendEmail);

        int GetMarketRiskAssessmentID();

        List<object> UpdateOnBoardingStatus(List<OnBoardingTrainingStatus> obj);
        List<object> UpdateOnBoardingStatus(OnBoardingTrainingStatus obj);

        bool EmilOnBoardingStatus();

        void GetOnBoardingProfile();

        List<OnboardingHelp> GetOnboardingHelp();

        List<Training> GetTrainings(int skillId, int competenceId);

        List<UserTraining> GetUserTrainingsByTrainingID(int TrainingId);

        List<Assessment> GetAssessments(int skillId, int competenceId);

        List<UserAssessment> GetUserAssessmentsByAssessmentId(int AssessmentId);

        void AddSkillBasedTrainingAssessment(string competence, int skillId, int userId);

        void AddSkillBasedTrainingAssessment(string competence, int skillId, int userId, bool isTrainingMandatory, DateTime lastDayOfCompletion);

        List<GEO> GetAllGEOs();
        List<Role> GetAllRoles();

        bool OnboardEmail(string email, int UserId, string UserName);

        void OnBoardUser(string competence, int skillId, int userId, string geo,int roleId, string userEmail, string userName);

        UserOnBoarding GetOnBoardingDetailsForUser(UserManager user);

        List<UserAssessment> GetAssessmentForUser(int userId, bool OnlyOnBoardedTraining = false);

        bool AssignAssessmentsToUser(List<UserAssessment> assessments, int userId, bool forDefault = false);

        List<UserManager> GetAllOnBoardedUser(int assignedTo);

        bool AssignTrainingsToUser(List<UserTraining> trainings, int userId, bool forDefault = false);

        bool AddSkill(string email, string userId, string skillId, string competence, bool ismandatory, DateTime lastdayofcompletion);

        bool AddRole(string email, string userId, string roleId,bool ismandatory, DateTime lastdayofcompletion);

        List<UserSkill> GetSkillForUser(int userId);

        List<UserSkill> GetSkillForOnboardedUser(int userId);
        List<UserRole> GetRoleForOnboardedUser(int userId);

        void UpdateUserSkill(int itemId, string competence, int userId, DateTime completiondate, bool isCompetenceChanged);

        List<UserOnBoarding> GetOnBoardingDetailsReport(string status, bool isExcelDownload);

        List<Competence> GetCompetenciesBySkillId(int SkillID);

        List<Competence> GetCompetenciesBySkillName(string name);

        void RemoveUserSkill(int itemId, string userId);
        void RemoveUserRole(int roleId, string userId);

        List<UserAssessment> GetUserAssessmentsByID(int ID);

        string GetUserCompetencyLabel(int userid);

        List<AcademyJoinersCompletion> GetCurrentUserAssessments(string listName, int? Id, bool updateAttempt);

        List<UserTrainingDetail> GetTrainingItems();

        List<UserSkillDetail> GetUserTrainingsDetails(string SPlistName);

        List<UserSkillDetail> GetTrainingJourneyDetails(string SPlistName, int userId);

        ProjectResources GetExpectedProjectResourceCountByProjectId(ProjectResources prjRes);

        void AddExpectedProjectResourceCountByProjectId(ProjectResources prjRes);

    }
}