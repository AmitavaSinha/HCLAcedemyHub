using HCL.Academy.Web.Models;
using System.Collections.Generic;

namespace HCLAcademy.Models
{
    public class ProjectData
    {

        public List<Project> projects { get; set; }
        public List<ProjectSkill> projectSkills { get; set; }
        public List<ProjectSkillResource> projectSkillResources { get; set; }

    }

    public class ProjectSkillResource
    {
        public string ProjectName { get; set; }
        public int ProjectId { get; set; }
        public string Skill { get; set; }
        public int SkillId { get; set; }
        public string CompetencyLevel { get; set; }
        public int CompetencyLevelId { get; set; }
        public string ExpectedResourceCount { get; set; }
        public string AvailableResourceCount { get; set; }
    }
}