using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HCLAcademy.Models
{
    public class ProjectDetails
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        public List<ProjectSkill> ProjectSkill { get; set; }
    }
    public class ProjectSkill
    {
        public string Project { get; set; }
        public string Skill { get; set; }
        public int ProjectId { get; set; }
        public int SkillId { get; set; }
        public int ItemId { get; set; }
    }
}