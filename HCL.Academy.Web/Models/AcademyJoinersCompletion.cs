using System;

namespace HCLAcademy.Models
{
    public class AcademyJoinersCompletion
    {
        public AcademyJoinersCompletion()
        {
            TrainingModuleLookupText = string.Empty;
            TrainingAssessmentLookUpText = string.Empty;
            TrainingCourseLookUpText = string.Empty;

        }
        public int Id { get; set; }
        public string Title { get; set; }
        public int TrainingCourseLookUpId { get; set; }
        public string TrainingCourseLookUpText { get; set; }
        public int TrainingModuleLookUpId { get; set; }
        public string TrainingModuleLookupText { get; set; }
        public int TrainingAssessmentLookUpId { get; set; }
        public string TrainingAssessmentLookUpText { get; set; }
        public int TrainingAssessmentTimeInMins { get; set; }
        public bool IsMandatory { get; set; }
        public bool IsTrainingLink { get; set; }
        public string TrainingLink { get; set; }
        public DateTime LastDayCompletion { get; set; }
        public DateTime CompletedDate { get; set; }
        public int MarksSecured { get; set; }
        public bool CertificateMailSent { get; set; }
        public int Attempts { get; set; }
        public int MaxAttempts { get; set; }
        public bool AssessmentStatus { get; set; }
        public string CompletionDate { get; set; }
    }
}