using System.Collections.Generic;

namespace HCLAcademy.Models
{
    public class UserManager
    {
        public bool IsOnline { get; set; }

        public int SPUserId { get; set; }

        public string UserName { get; set; }

        public string EmailID { get; set; }

        public bool HasAttachments { get; set; }

        public int OnBoardingID { get; set; }

        public string FileName { get; set; }

        public string Manager { get; set; }

        public List<string> Peers { get; set; }

        public List<string> Reportees { get; set; }

        public string Designation { get; set; }

        public bool isSiteAdmin { get; set; }

        public int GroupPermission { get; set; }

        public List<string> Groups { get; set; }

        //Added for Assessment
        public bool IsAssessmentCompleted { get; set; }

        public bool IsNewJoinee { get; set; } 

        public List<string> TrainingsToComplete { get; set; } 

        public List<string> TrainingsCompleted { get; set; } 

        public string Competency { get; set; }

        public int DBUserId { get; set; }
    }
}