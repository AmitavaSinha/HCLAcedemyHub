using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HCLAcademy.Models
{
    public class Resource
    {
        public string projectName { get; set; }
        public int projectId { get; set; }
        
        public List<AllResources> allResources { get; set; }
    }
}