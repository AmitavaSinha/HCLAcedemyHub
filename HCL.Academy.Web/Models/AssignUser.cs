using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using HCLAcademy.Models;

namespace HCLAcademy.Models
{
    public class AssignUser
    {
        public List<Project> lstProjects { get; set; }

        public List<Users> lstUsers { get; set; }
        [Required(ErrorMessage = "Please select a User")] 
        public string selectedUser { get; set; }
        [Required(ErrorMessage = "Please select a Project")]
        public string selectedProject { get; set; }

        public AssignUser()
        {
            lstProjects = new List<Project>();
            lstUsers = new List<Users>();
        }
    }
}