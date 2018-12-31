using System.Collections.Generic;

namespace HCLAcademy.Models
{
    public class UserOnBoarding
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public List<GEO> GEOs { get; set; }
        public List<Role> Roles { get; set; }
        public List<Skill> Skills { get; set; }
        public List<Competence> Competence { get; set; }
        public List<Assessment> Assessments { get; set; }
        public List<Training> Trainings { get; set; }
        public List<Status> Status { get; set; }
        public List<BGVStatus> BGVStatus { get; set; }
        public List<ProfileSharing> ProfileSharingStatus { get; set; }
        public string CurrentSkill { get; set; }
        public string CurrentCompetance { get; set; }
        public string CurrentStatus { get; set; }
        public string CurrentProfileSharing { get; set; }
        public string CurrentBGVStatus { get; set; }
        public string CurrentGEO { get; set; }
        public string CurrentRole { get; set; }
        //public bool IsNLWorkPermit { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        public List<UserTraining> UserTrainings { get; set; }
        public List<UserAssessment> UserAssessments { get; set; }

        public List<UserSkill> UserSkills { get; set; }
        public bool IsPresentInOnBoard { get; set; }
        public List<Project> Projects { get; set; }
    }

    public class Skill
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; }
    }
    public class GEO
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }
    public class Role
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public class Training
    {
        public int TrainingId { get; set; }
        public int SkillId { get; set; }
        public int CompetencyId { get; set; }
        public string TrainingName { get; set; }
        public bool IsMandatory { get; set; }
    }
    public class RoleTraining
    {
        public int TrainingId { get; set; }
        public int RoleId { get; set; }        
        public string TrainingName { get; set; }
        public bool IsMandatory { get; set; }
        public string URL { get; set; }
        public int Points { get; set; }
        public string Description { get; set; }

    }
    public class Assessment
    {
        public int AssessmentId { get; set; }
        public string AssessmentName { get; set; }
        public string Description { get; set; }
        public int TrainingId { get; set; }
        public bool IsMandatory { get; set; }
        public string AssessmentLink { get; set; }

        public int AssessmentTimeInMins { get; set; }
        public int SkillId { get; set; }
        public int CompetencyId { get; set; }
    }

    public class Competence
    {
        public int CompetenceId { get; set; }
        public string CompetenceName { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public string Description { get; set; }
        public int TrainingCompletionPoints { get; set; }
        public int AssessmentCompletionPoints { get; set; }
        public int CompetencyLevelOrder { get; set; }
    }

    public class Status
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; }
    }
    public class BGVStatus
    {
        public int BGVStatusId { get; set; }
        public string BGVStatusName { get; set; }
    }
    public class ProfileSharing
    {
        public int ProfileSharingId { get; set; }
        public string ProfileSharingName { get; set; }
    }
    public class UserSkill
    {
        public int Id { get; set; }
        public string Employee { get; set; }
        public string Skill { get; set; }
        public string Competence { get; set; }
        public string SkillwiseCompetencies { get; set; }
        public string SkillwiseCompetencyIds { get; set; }
        public string ProfessionalSkills { get; set; }
        public string SoftSkills { get; set; }
        public int SkillId { get; set; }
        public string LastDayCompletion { get; set; }
        public int CompetenceId { get; set; }
        public int UserId { get; set; }
    }

    public class UserTraining
    {
        // For AcademyJoinersTraining List
        public string Employee { get; set; }
        public int SkillId { get; set; }// Course of the training like Java, PEGA etc
        public string SkillName { get; set; }
        public int TrainingId { get; set; } // Name of the Training
        public string TrainingName { get; set; }
        public bool IsMandatory { get; set; }        
        public bool IsTrainingCompleted { get; set; }
        public bool IsTrainingActive { get; set; }
        public bool IsIncludeOnBoarding { get; set; }
        public string LastDayCompletion { get; set; }
        public string CompletedDate { get; set; }
        public string StatusColor { get; set; }
        public string ItemStatus { get; set; }
        public int UserId { get; set; }
        public int Id { get; set; }
    }

    public class UserAssessment
    {
        public int TrainingAssessmentId { get; set; } // Assessment Id
        public string TrainingAssessment { get; set; } // Assessment Name
        public int SkillId { get; set; } // Course Id like Java etc
        public string SkillName { get; set; }
        public int TrainingId { get; set; } // Trainging Id
        public string TrainingName { get; set; } // Training Name
        public bool IsMandatory { get; set; }
        public bool IsAssessmentComplete { get; set; }
        public bool IsAssessmentActive { get; set; }
        public bool IsIncludeOnBoarding { get; set; }
        public string LastDayCompletion { get; set; }
        public string CompletedDate { get; set; }

        public string StatusColor { get; set; }
        public string ItemStatus { get; set; }

        public string Employee { get; set; }

        public decimal MarksInPercentage { get; set; }
        public int UserId { get; set; }
        public int Id { get; set; }
    }
}