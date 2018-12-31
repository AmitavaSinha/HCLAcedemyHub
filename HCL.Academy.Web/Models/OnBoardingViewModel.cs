using System.Collections.Generic;

namespace HCLAcademy.Models
{
    public class OnBoardingViewModel
    {
        public List<OnBoarding> TopRowList { get; set; }
        public List<OnBoarding> BottomRowList { get; set; }

        public List<bgColor> bgColorList { get; set; }
        public bool sendEmail { get; set; }
    }
}