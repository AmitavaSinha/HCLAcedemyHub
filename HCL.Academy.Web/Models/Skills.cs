using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HCLAcademy.Models;


namespace HCLAcademy.Models
{
    public class Skills
    {
        public string skillName { get; set; }
        public List<SkillCompetencies> competences { get; set; }
    }

    public class SkillCompetencies
    {
        public int CompetenceId { get; set; }
        public string CompetenceName { get; set; }
        public int SkillId { get; set; }
        public string Description { get; set; }
        public string TrainingName { get; set; }
        public string TrainingDescription { get; set; }
        public string TrainingLink { get; set; }
        public int TrainingId { get; set; }
    }
}