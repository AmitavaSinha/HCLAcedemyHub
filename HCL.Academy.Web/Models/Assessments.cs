using System.Collections.Generic;

namespace HCLAcademy.Models
{
    public class Assessments
    {
        public int AssessmentId { get; set; }
        public string AssessmentName { get; set; }  // Form AcademyModules
        public List<QuestionDetail> QuestionDetails { get; set; } //From AcademyAssessments
        public string PassingMarks { get; set; }  // From AcademyModules
        public string AssessmentStatus { get; set; }  // From
        public string SecuredMarks { get; set; }
        public int TotalMarks { get; set; }
        public bool MaxAttemptsExceeded { get; set; }
        public bool AssessmentCompletionStatus { get; set; }
        public int PassingPercentage { get; set; }
        public List<AcademyJoinersCompletion> AssessmentDetails { get; set; }

    }
    public class AssesmentResult
    {
        public int SecuredMarks { get; set; }
        public int AssessmentId { get; set; }
        public int TotalMarks { get; set; }
        public int PassingPercentage { get; set; }
    }

    
}