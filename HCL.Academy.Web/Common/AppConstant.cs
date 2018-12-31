namespace HCLAcademy.Common
{
    public class SessionConstant
    {
        #region Sessions
        public static string CurrentUser = "CurrentUser";
        public static string ClientContext = "ClientContext";
        #endregion
    }

    public class AppConstant
    {
        private AppConstant() { }

        #region ListName Constants
        public static string EmailTemplate = "EmailTemplate"; 
        public static string EmailQueue = "EmailQueue";
        public static string RSSFeeds = "RssFeeds"; 
        public static string AcademyOnBoarding = "AcademyOnBoarding";
        public static string UserAssessmentMapping = "UserAssessmentMapping"; //AcademyJoinersCompletion
        public static string UserTrainingMapping = "UserTrainingMapping"; //AcademyJoinersTraining
        public static string AcademyAssessment = "AssessmentQuestions"; //AcademyAssessment
        public static string TrainingModules = "TrainingModules";
        public static string Assessments = "Assessments"; //TrainingAssessment
        public static string TrainingCourses = "TrainingCourses";
        public static string DefaultOnboardingTrainingAssessment = "DefaultOnboardingTrainingAssessment";

        public static string AcademyConfig = "AcademyConfig";
        public static string AcademyEvents = "AcademyEvents";
        public static string AcademyNews = "AcademyNews";
        public static string AcademyOnBoardingColumnSchema = "AcademyOnBoardingColumnSchema";
        public static string FAQs = "FAQs";  //AcademyPolicies 

        public static string AcademyVideos = "AcademyVideos";
        public static string WikiDocuments = "WikiDocuments"; //OnBoarding
        public static string SiteMenu = "SiteMenu";

        
        public static string TrainingPlan = "TrainingPlan";
        //public static string AcademyPMO = "StateStreet PMO";
        


        
        
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

        public static string OnboardingHelp = "OnboardingHelp";
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
        public static string TrainingModule_TrainingName = "ModuleName";
        public static string TrainingModule_TrainingDescription = "TrainingDescription";
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