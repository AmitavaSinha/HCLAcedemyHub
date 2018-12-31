namespace HCLAcademy.Models
{
    public class TrainingModules
    {
        public int Id { get; set; }
        public string ModuleName { get; set; }
        public string AssessmentName { get; set; }
        public int AssessmentPassingPercentage { get; set; }
        public int AssessmentTimeInMins { get; set; }
    }
}