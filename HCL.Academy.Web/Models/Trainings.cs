using System;
using System.ComponentModel.DataAnnotations;

namespace HCLAcademy.Models
{
    public class UserTrainingDetail
    {
        public int Id { get; set; }
        public string SkillName { get; set; }
        public int SkillId { get; set; }
        public string TrainingName { get; set; }
        public int TrainingId { get; set; }
        public string ModuleDesc { get; set; }
        public string DocumentUrl { get; set; }
        public bool Mandatory { get; set; }
        //added for Link purpose
        public bool IsLink { get; set; }
        public string LinkUrl { get; set; }
        //NoOfattempts & lastDaytoComplete & PassedStatus?
        public bool IsWikiLink { get; set; }
        public int NoOfAttempts { get; set; }
        public DateTime LastDayToComplete { get; set; }
        public bool IsTrainingCompleted { get; set; }
        public TraningStatus status { get; set; }
        public Colors bgColor { get; set; }
        public string CompletionDate { get; set; }
        public TrainingType TrainingType { get; set; }
    }

    public enum TraningStatus
    {
        [Display(Name = "On Going")]
        OnGoing,

        [Display(Name = "Completed")]
        Completed,

        [Display(Name = "Over Due")]
        OverDue,

        [Display(Name = "Failed")]
        Failed
    }

    public enum TrainingType
    {
        SkillTraining,
        RoleTraining
    }

}