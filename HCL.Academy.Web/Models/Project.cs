
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCLAcademy.Models
{
    public class Project
    {
        public int ID { get; set; }
        [Required(ErrorMessage = "Project Name is Required")]
        public string ProjectName { get; set; }

        public List<Skills> lstSkills { get; set; }
        
    }
    

}