namespace HCLAcademy.Common
{
    public class SessionConstant
    {
        #region Sessions
        public static string CurrentUser = "CurrentUser";
      //  public static string ClientContext = "ClientContext";
        #endregion
    }

    public class AppConstant
    {
        private AppConstant() { }

        #region ListName Constants
        public static string EmailTemplate = "Email Template"; 
        public static string EmailQueue = "EmailQueue";
        public static string RSSFeeds = "RssFeeds"; 
        public static string AcademyOnBoarding = "Academy OnBoarding";
        public static string UserAssessmentMapping = "User Assessments"; //AcademyJoinersCompletion
        public static string UserTrainingMapping = "User Trainings"; //AcademyJoinersTraining
        public static string UserRoleTraining = "UserRoleTraining"; 
        public static string AcademyAssessment = "Assessment Questions"; //AcademyAssessment
        public static string SkillCompetencyLevelTrainings = "Skill Competency Level Trainings";
        public static string RoleTraining = "RoleTraining";
        public static string SkillCompetencyLevels = "Skill Competency Levels";
        public static string Assessments = "Assessments"; //TrainingAssessment
        public static string Skills = "Skills";
        public static string DefaultOnboardingTrainingAssessment = "Default Onboarding Assessment";
        public static string AcademyGEO = "AcademyGEO";
        public static string AcademyConfig = "AcademyConfig";
        public static string AcademyEvents = "Academy Events";
        public static string AcademyNews = "Academy News";
        public static string AcademyOnBoardingColumnSchema = "Academy Schema";
        public static string AcademyOnBoardingColumnSchemaInternalName = "AcademySchema";
        public static string FAQs = "FAQs";  //AcademyPolicies 
        public static string AcademyVideos = "Academy Videos";
        public static string WikiDocuments = "Training Documents"; //OnBoarding
        public static string SiteMenu = "Site Menu";
        public static string Roles = "Roles";


        public static string TrainingPlan = "Training Content";
        public static string UserSkills = "User Skills";
        //public static string AcademyPMO = "StateStreet PMO";
        public static string OnBoardingCheckList = "OnBoardingCheckList";
        public static string UserPoints = "UserPoints";
        public static string UserAssessmentHistory = "User Assessments History";
        public static string Projects = "Projects";
        public static string ProjectSkills = "ProjectSkills";
        public static string ProjectSkillResource = "ProjectSkillresource";
        public static string UserCheckList = "UserCheckList";




        #endregion

        #region RAG Status
        public static string Red = "Red";
        public static string Amber = "Orange";
        public static string Green = "Green";

        #endregion

        #region Item Status
        public static string Completed = "Completed";
        public static string InProgress  = "In Progress";
        public static string OverDue = "OverDue";
        #endregion

        public static int MaxQueForAssessment = 40;
        public static string OnboardingHelp = "Onboarding Help";
        public static string DefaultTrainingAssessmentData = "DefaultTrainingAssessmentData";
        public static string AllGEOData = "AllGEOData";
        public static string AllRoleData = "AllRoleData";
        public static string AllSkillData = "AllSkillData";
        public static string AllCompetencyData = "AllCompetencyData";
        public static string AllWikiPolicyData = "AllWikiPolicyData";
        public static string AllVideoData = "AllVideoData";
        public static string AllEventsData = "AllEventsData";
        public static string AllTrainingData = "AllTrainingData";
        public static string AllRoleTrainingData = "AllRoleTrainingData";
        public static string AllAssessmentData = "AllAssessmentData";
        public static string AllCheckListData = "AllCheckListData";
        public static string StorageTableLogs = "Logs";
        public static string ApplicationName = "AcademyPortal";
        public static string PartitionError = "Error";
        public static string PartitionInformation = "Information";
        public static string ErrorLogEntities = "ErrorLogEntities";
    }

    public static class ListConstant
    {
        #region Lists
        public static string Competencies = "Competencies";
        public static string TrainingMappingDetails = "TrainingMappingDetails";
        public static string SiteAssets = "Site Assets";
        public static string BannerFolder = "Banners";
        public static string LogoFolder = "logo";
        public static string LandingPageHeader = "LandingPageHeader";
        #endregion
    }

    public class FieldConstant
    {
        #region INGCompetencies List Fields
        public static string Competencies_SkillTypes = "SkillTypes";
        public static string Competencies_SkillHeader = "SkillHeader";
        public static string Competencies_SkillDescription = "SkillDescription";
        #endregion

        #region TrainingModule List Fields
        public static string TrainingModule_TrainingName = "Title";
        public static string TrainingModule_TrainingDescription = "Description1";
        public static string TrainingModule_IsWikiLink = "IsWikiLink";
        public static string TrainingModule_TrainingLink = "TrainingLink";
        public static string TrainingModule_SkillType = "SkillType";
        #endregion

        #region AcademyOnBoarding List Fields
        public static string AcademyOnBoarding_Skill = "Skill";
        #endregion

        public static string EncodedAbsUrl = "EncodedAbsUrl";
        public static string FileRef = "FileRef";
        public static string FileLeafRef = "FileLeafRef";
        
        public static string Banners_Ordering = "ordering";
    }

    public class FieldValueConstant
    {
        #region TrainingModule SkillType Field Value
        public static string TrainingModule_SkillType_SoftSkill = "Soft Skill";
        #endregion

        #region Competencies SkillType Field Value
        public static string Competencies_SkillTypes_SoftSkill = "Soft Skills";
        #endregion
    }

    public class MessageConstant
    {
        public static string AssesmentConfirmMsg = "Are you sure you want to start the assesment now? If yes please click OK to confirm else click Cancel. Otherwise you will loose an attempt.";
    }

    public class GroupConstant
    {
        public static string MarketRiskMembersGroup = "Market Risk Members";
    }
}