using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HCLAcademy.Models
{
    public class TrainingReport
    {
        public string selectedSkill { get; set; }
        public string selectedCompetence { get; set; }
        public List<UserDetails> userDetails { get; set; }
        public List<TrainingCount> counts { get; set; }

    }

    public class UserDetails
    {
        public int TrainingId { get; set; }
        public string TrainingName { get; set; }//Name of the Training
        public bool IsMandatory { get; set; }
        public string Employee { get; set; }
        public int TrainingCourseId { get; set; }// Course of the training like Java, PEGA etc
        public string TrainingCourse { get; set; }
        public int TrainingModuleId { get; set; } // Name of the Training
        public string TrainingModule { get; set; }
        public bool IsTrainingCompleted { get; set; }
        public bool IsTrainingActive { get; set; }
        public bool IsIncludeOnBoarding { get; set; }
        public string LastDayCompletion { get; set; }
        public string CompletedDate { get; set; }
        public string StatusColor { get; set; }
        public string ItemStatus { get; set; }
        public string skillName { get; set; } //Skill associated with the Training
        public string competenceName { get; set; } //Competency Level of the Training

    }

    public class TrainingCount
    {
        public string competencyLevel { get; set; }
        public string trainingName { get; set; }
        public int completedCount { get; set; }
        public int progressCount { get; set; }
        public bool isTrainingComplete { get; set; }
    }
}