using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HCLAcademy.Models
{
    public class AllResources
    {
        public string skill { get; set; }
        public int skillId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        [Display(Name = "Novice Count")]
        public int expectedBeginnerCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        [Display(Name = "Novice Count")]
        public int availableBeginnerCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        [Display(Name = "Advanced Beginner Count")]
        public int ExpectedadvancedBeginnerCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        [Display(Name = "Advanced Beginner Count")]
        public int AvailableadvancedBeginnerCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        [Display(Name = "Competent Count")]
        public int ExpectedcompetentCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        [Display(Name = "Competent Count")]
        public int AvailablecompetentCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        [Display(Name = "Proficient Count")]
        public int expectedProficientCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        [Display(Name = "Proficient Count")]
        public int availableProficientCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        [Display(Name = "Expert Count")]
        public int expectedExpertCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer Number")]
        [Display(Name = "Expert Count")]
        public int availableExpertCount { get; set; }
    }
}