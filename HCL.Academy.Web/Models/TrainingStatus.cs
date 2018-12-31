using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HCL.Academy.Web;
 using HCLAcademy.Models;

namespace HCLAcademy.Models
{
    public class TrainingStatus
    {
        public List<Competence> Competence { get; set; }        
        public List<Skill> Skills { get; set; }
    }
}