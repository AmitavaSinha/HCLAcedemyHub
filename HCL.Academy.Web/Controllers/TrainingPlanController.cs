using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace HCLAcademy.Controllers
{
    public class TrainingPlanController : Controller
    {
        [Authorize]
        [SessionExpire]
        public ActionResult TrainingPlan()
        {
            return View();
        }

        [SessionExpire]
        public PartialViewResult TrainingContents(int id)
        {
            //IDAL dal = (new DALFactory()).GetInstance();
            SharePointDAL dal = new SharePointDAL();
            List<TrainingPlan> trainingPlans = dal.GetTrainingPlans(id);        //Get the Training Plan based on ID.
            return PartialView("_TrainingPlanContent", trainingPlans);
        }

        [SessionExpire]
        public PartialViewResult Training()
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<UserSkillDetail> traningModules = dal.GetUserTrainingsDetails(
                    AppConstant.UserTrainingMapping);       //Gets a list of all User Trainings.
                return PartialView("_TrainingPanel", traningModules);
        }

        private static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, string.Empty);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }
        /// <summary>
        /// Fetches the Training Report for particular user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        //[Authorize]
        //[HttpPost]
        //[SessionExpire]
        //public JsonResult GetTrainingsForReport(int userId, string userEmail)
        //{
        //    //UserManager user = (UserManager)Session["CurrentUser"];
        //    IDAL dal = (new DALFactory()).GetInstance();
        //    List<UserTraining> trainings = dal.GetTrainingForUser(user.UserId, true);
        //    return new JsonResult { Data = trainings };
        //}
        /// <summary>
        /// Gets the list of trainings for the selected user in a popup.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [SessionExpire]
        public JsonResult GetTrainingsFoPopUp()
        {
            UserManager user = (UserManager)System.Web.HttpContext.Current.Session["CurrentUser"];
            OnBoardingViewModel boardingViewModel = new OnBoardingViewModel();
            IDAL dal = (new DALFactory()).GetInstance();
            List<OnBoarding> listOnboarding = dal.GetBoardingData();
            if (listOnboarding.Count > 0)
            {
                boardingViewModel.TopRowList = listOnboarding.ToList().Where((c, i) => i % 2 == 0).ToList();
                boardingViewModel.BottomRowList = listOnboarding.ToList().Where((c, i) => i % 2 != 0).ToList();
                boardingViewModel.bgColorList = GetBgColor(listOnboarding);          
            }
            return new JsonResult { Data = listOnboarding };
        }
        /// <summary>
        /// Gets the backgroud color based on status.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private List<bgColor> GetBgColor(List<OnBoarding> list)
        {
            List<bgColor> bgColorList = new List<bgColor>();

            foreach (OnBoarding item in list)
            {
                switch (item.BoardingStatus)
                {
                    case OnboardingStatus.NotStarted:
                        bgColorList.Add(bgColor.blue);
                        break;

                    case OnboardingStatus.OnGoing:
                        bgColorList.Add(bgColor.orange);
                        break;

                    case OnboardingStatus.Completed:
                        bgColorList.Add(bgColor.green);
                        break;

                    case OnboardingStatus.Rejected:
                        bgColorList.Add(bgColor.red);
                        break;

                    case OnboardingStatus.Failed: //Added Failed - Sudipta
                        bgColorList.Add(bgColor.red);
                        break;

                    case OnboardingStatus.OverDue: //Added Failed - Sudipta
                        bgColorList.Add(bgColor.red);
                        break;

                    default:
                        bgColorList.Add(bgColor.blue);
                        break;
                }
            }
            return bgColorList;
        }

    }
}