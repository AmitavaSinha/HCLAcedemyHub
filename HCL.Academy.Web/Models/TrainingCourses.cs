using System;
using System.Collections.Generic;

namespace HCLAcademy.Models
{    
    public class UserSkillDetail
    {
        public string skillName { get; set; }
        public int id { get; set; }
        public bool isDefault { get; set; }
        public CourseStatus skillStatus { get; set; }
        public DateTime LastDayToComplete { get; set; }
        public List<UserTrainingDetail> listOfTraining { get; set; }
        public TrainingType TrainingType { get; set; }
    }  
}