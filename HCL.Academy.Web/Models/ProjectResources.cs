using System.Collections.Generic;

namespace HCLAcademy.Models
{
    public class ProjectResources
    {
        public string projectName { get; set; }
        public int projectId { get; set; }

        public List<SkillResource> skillResources { get; set; }
    }
}