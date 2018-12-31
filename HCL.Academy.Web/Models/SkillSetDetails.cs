using System.Collections.Generic;

namespace HCLAcademy.Models
{
    public class SkillSetDetails
    {
        public string competencyLevel { get; set; }
        public List<SkillDetails> SoftSkillSet { get; set; }
        public List<SkillDetails> ProfessionalSkillSet { get; set; }
        public List<TrainingDetails> SoftSkillTrainings { get; set; }
        public List<TrainingDetails> ProfessionalSkilltrainings { get; set; }
    }

    public class SkillDetails
    {
        public string SkillHeader { get; set; }
        public string SkillDescription { get; set; }
    }

    public class TrainingDetails
    {
        public string TrainingName { get; set; }
        public string TrainingDescription { get; set; }
        public bool IsInternalTraining { get; set; }
        public string TrainingLink { get; set; }
    }
}