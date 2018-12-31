using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using HCLAcademy.Utility;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Web.Mvc;
using System.Web.UI;

namespace HCLAcademy.Controllers
{
    public class HomeController : Controller
    {
        // private ClientContext _clientContext;

        [Authorize]
        [SessionExpire]
        //[OutputCache(Duration = 600, VaryByCustom = "User", VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public ActionResult Home()
        {
            IDAL dal = (new DALFactory()).GetInstance();

            CacheSiteMenu();

            dal.CacheConfig();

            CacheLogo();

            BannersList bannersList = null;
            string bannerSource = ConfigurationManager.AppSettings["BannerSource"].ToString();
            if (bannerSource.Equals("SharePoint"))
            {
                bannersList = FetchAndCacheBanners();
            }

            return View(bannersList);
        }
        /// <summary>
        /// Fetch the banners displayed on the home page
        /// </summary>
        /// <returns></returns>
        private BannersList FetchAndCacheBanners()
        {
            BannersList bannersList = new BannersList();
            try
            {
                if (Session["BannersList"] == null)
                {
                    //IDAL dal = (new DALFactory()).GetInstance();
                    SharePointDAL dal = new SharePointDAL();
                    bannersList.BannersListdetails = dal.GetBanners();
                    Session["BannersList"] = bannersList;
                }
                else
                {
                    bannersList = (BannersList)Session["BannersList"];
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Home,FetchAndCacheBanners", ex.Message, ex.StackTrace));
            }
            return bannersList;
        }
        /// <summary>
        /// Get the Navigation pane of the website
        /// </summary>
        private void CacheSiteMenu()
        {
            try
            {
                if (Session["UserSiteMenu"] == null)
                {
                    IDAL dal = (new DALFactory()).GetInstance();
                    List<SiteMenu> siteMenu = dal.GetMenu();
                    Session["UserSiteMenu"] = siteMenu;
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Home,CacheSiteMenu", ex.Message, ex.StackTrace));
            }
        }
        /// <summary>
        /// Checks whether a group is authorized to access the website
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        public ActionResult UnAuthorized(string groupName)
        {
            ViewBag.groupName = groupName;
            return View();
        }

        /// <summary>
        /// Gets the Learning graph/journey of the logged in user.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByCustom = "User", VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public PartialViewResult GetLearningJourney()
        {
            List<UserSkillDetail> userLearningJourney = new List<UserSkillDetail>();
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                UserManager user = (UserManager)Session["CurrentUser"];
                string dataStore = ConfigurationManager.AppSettings["DATASTORE"].ToString();
              
                if (dataStore == "SqlSvr")
                {
                    SqlSvrDAL sqlDAL = new SqlSvrDAL();
                    int dbUserId = sqlDAL.GetUserId(user.EmailID);
                    userLearningJourney = dal.GetTrainingJourneyDetails(AppConstant.UserTrainingMapping, dbUserId);
                    user.Competency = dal.GetUserCompetencyLabel(user.DBUserId);
                }
                else
                {
                    userLearningJourney = dal.GetTrainingJourneyDetails(AppConstant.UserTrainingMapping, user.SPUserId);
                    user.Competency = dal.GetUserCompetencyLabel(user.SPUserId);
                }

                Session.Add("CurrentUser", user);
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Home,CacheSiteMenu", ex.Message, ex.StackTrace));
            }
            return PartialView("_LearningJourney", userLearningJourney);
        }

        /// <summary>
        /// Get the profile of the logged in user
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByCustom = "User", VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public PartialViewResult GetMyProfile()
        {
            //IDAL dal = (new DALFactory()).GetInstance();
            SharePointDAL dal = new SharePointDAL();
            UserManager user = new UserManager();
            try
            {
                user = dal.GetCurrentUserCompleteUserProfile();
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Home,CacheSiteMenu", ex.Message, ex.StackTrace));
            }
            return PartialView("_MyProfile", user);
        }

        /// <summary>
        /// Get a list of trainings
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByCustom = "User", VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public ActionResult GetTrainings()
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<UserTrainingDetail> ListOfTrainings = new List<UserTrainingDetail>();
            try
            {
                ListOfTrainings = dal.GetTrainingItems();       //Training items of the logged in user
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Home,CacheSiteMenu", ex.Message, ex.StackTrace));
            }
            return PartialView("_Trainings", ListOfTrainings);
        }

        /// <summary>
        /// Get the assessments of the logged in User
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByCustom = "User", VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public ActionResult GetAssessments()
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<AcademyJoinersCompletion> ListOfAssessments = dal.GetCurrentUserAssessments(
                AppConstant.UserAssessmentMapping, null, false);                //Get the assessments of the logged in User
            return PartialView("_Assessments", ListOfAssessments);
        }
        /// <summary>
        /// Get the list of News items.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public ActionResult GetNewsItems()
        {
            //IDAL dal = (new DALFactory()).GetInstance();
            SharePointDAL dal = new SharePointDAL();
            string noImagePath = Server.MapPath(Url.Content("~/Images/noimage.png"));
            List<News> NewsList = dal.GetNews(noImagePath);         //Gets the list of News items to be displayed in the Home Page.
            return PartialView("_News", NewsList);
        }

        /// <summary>
        /// RSS Feeds for the Home Page
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public ActionResult GetRssNewsItems()
        {
            List<RSSFeed> postRSList = new List<RSSFeed>();
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                postRSList = dal.GetRSSFeeds();     //Get the RSS Feeds for the Home Page
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Home, GetRssNewsItems", ex.Message, ex.StackTrace));

                //AKPALthrow ex;
            }
            return PartialView("_RssFeed", postRSList);
        }

        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public void CacheLogo()
        {
            try
            {
                if (Session["LogoBase64Image"] == null)
                {
                    SharePointDAL dal = new SharePointDAL();
                    string logoSource = ConfigurationManager.AppSettings["LogoSource"].ToString();
                    if (logoSource.Equals("SharePoint"))
                    {
                        Session["LogoBase64Image"] = dal.GetBase64BitLogoImageStream();
                    }
                    else
                    {
                        Session["LogoBase64Image"] = ConfigurationManager.AppSettings["ClientLogo"].ToString();
                    }

                }
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Home,CacheLogo", ex.Message, ex.StackTrace));
            }
        }

        /// <summary>
        /// Events to be displayed on the Home Page
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public ActionResult GetEventsItems()
        {
            //IDAL dal = (new DALFactory()).GetInstance();
            SharePointDAL dal = new SharePointDAL();
            List<AcademyEvent> academyEvents = dal.GetEvents();         //Get the list of events
            return PartialView("_Events", academyEvents);
        }

        /// <summary>
        /// Download a file from the specified file path
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        [Authorize]
        public FileResult DownloadFile(string filePath)
        {
            string decryptFileName = EncryptionHelper.Decrypt(filePath);
            SharePointDAL spDal = new SharePointDAL();
            Stream fileBytes = spDal.DownloadDocument(decryptFileName);
            try
            {
                string fileName = decryptFileName.Substring(decryptFileName.LastIndexOf('/') + 1);
                string url = ConfigurationManager.AppSettings["URL"].ToString();
                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Home,DownloadFile", ex.Message, ex.StackTrace));
                return null;
            }

        }
        /// <summary>
        /// Get the Certificate to be downloaded using ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public FileResult DownloadCertificate(int id)
        {
            IDAL dal = (new DALFactory()).GetInstance();
            SharePointDAL spDal = new SharePointDAL();
            try
            {
                string certificateHtml = string.Empty;
                MemoryStream workStream = new MemoryStream();
                string strPDFFileName = string.Format("Certificate.pdf");

                UserManager user = (UserManager)System.Web.HttpContext.Current.Session["CurrentUser"];
                user = spDal.GetUserByEmail(user.EmailID);

                List<UserAssessment> userAssessments = dal.GetUserAssessmentsByID(id);

                foreach (UserAssessment userAssessment in userAssessments)
                {
                    certificateHtml = "<HTML><HEAD><TITLE>Certificate of Completion</TITLE>"
                            + "<META content='text/html; charset=utf-8' http-equiv=Content-Type> <META name=viewport content='width=device-width, initial-scale=1.0'> <META name=GENERATOR content='MSHTML 11.00.9600.18698'></HEAD>"
                          + "<BODY> <TABLE style='PADDING: 5px; MARGIN: 10px 10%;  BACKGROUND-COLOR: #fee2cf'>"
                          + " <TR> <TD style='BORDER: #ff6600 10px solid; PADDING: 10px; margin:10px;'> <TABLE >"
                          + " <TR> <TD style='BORDER: #ff6600 2px solid; BACKGROUND-COLOR: #fff;'> <TABLE>"
                          + " <TR> <TD style=' BACKGROUND-COLOR: #ffffff'> <TABLE>"
                          + " <TR> <TD style='WIDTH: 100%;'>"
                              + " <TABLE style='WIDTH: 100%'>"

                                   + "<TR> <TD align=center> <TABLE>  <TR> <TD style='FONT-SIZE: 25px; FONT-FAMILY: georgia'>ING Academy</TD></TR></TABLE></TD></TR>"
                           + " <TR> <TD style='FONT-SIZE: 30px; FONT-FAMILY: Tahoma, Geneva, sans-serif; COLOR: #ff6200; TEXT-ALIGN: center; PADDING-TOP: 10px' align=center>Certificate of Completion</TD></TR>"
                           + " <TR> <TD style='FONT-SIZE: 14px; FONT-FAMILY: Tahoma, Geneva, sans-serif; COLOR: #666; TEXT-ALIGN: center; PADDING-TOP: 20px'>This is certify that</TD></TR> "
                                   + "<TR> <TD style='FONT-SIZE: 18px; FONT-FAMILY: \"Times New Roman\", Times, serif; FONT-WEIGHT: bold; COLOR: #000; TEXT-ALIGN: center'>" + user.UserName + "</TD></TR>"
                           + " <TR> <TD style='FONT-SIZE: 14px; FONT-FAMILY: Tahoma, Geneva, sans-serif; COLOR: #666; TEXT-ALIGN: center; PADDING-TOP: 10px'>has successfully completed</TD></TR>"
                           + " <TR> <TD style='FONT-SIZE: 16px; FONT-FAMILY: \"Times New Roman\", Times, serif; COLOR: #000; PADDING-BOTTOM: 30px; TEXT-ALIGN: center'>"
                                   + userAssessment.TrainingAssessment + "</TD></TR>"
                              + "</TABLE>"
                          + " <TABLE style='WIDTH: 100%'> "
                                + " <TR>"
                                       + "<TD style='HEIGHT: 50px; WIDTH: 150px; BACKGROUND-REPEAT: no-repeat; PADDING-LEFT: 10px'>"
                           + " Your Score - " + userAssessment.MarksInPercentage + "%</TD> "
                                       + "<TD style='FONT-SIZE: 14px; FONT-FAMILY: Tahoma, Geneva, sans-serif; COLOR: #000; TEXT-ALIGN: right; PADDING-TOP: 0px; PADDING-RIGHT: 10px'>"
                          + userAssessment.CompletedDate + "</TD></TR></TABLE>"
                          + "</TD></TR>"
                          + "</TABLE></TD></TR>"
                          + "</TABLE></TD></TR>"
                          + "</TABLE></TD></TR>"
                          + "</TABLE></BODY></HTML>";
                }

                Bitmap bitmap = new Bitmap(1800, 1800);
                Graphics g = Graphics.FromImage(bitmap);
                XGraphics xg = XGraphics.FromGraphics(g, new XSize(bitmap.Width, bitmap.Height));
                TheArtOfDev.HtmlRenderer.PdfSharp.HtmlContainer c = new TheArtOfDev.HtmlRenderer.PdfSharp.HtmlContainer();
                c.SetHtml(certificateHtml);

                PdfDocument pdf = new PdfDocument();
                PdfPage page = new PdfPage();
                XImage img = XImage.FromGdiPlusImage(bitmap);
                page.Size = PdfSharp.PageSize.A4;
                pdf.Pages.Add(page);
                XGraphics xgr = XGraphics.FromPdfPage(pdf.Pages[0]);
                c.PerformLayout(xgr);
                c.PerformPaint(xgr);
                xgr.DrawImage(img, 0, 0);
                pdf.Save(workStream, false);

                byte[] byteInfo = workStream.ToArray();
                workStream.Write(byteInfo, 0, byteInfo.Length);
                workStream.Position = 0;
                return File(workStream, "application/pdf", strPDFFileName);
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Home,DownloadCertificate", ex.Message, ex.StackTrace));
                return null;
            }
        }

        /// <summary>
        /// Download the profile of the current user
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public FileResult DownloadProfile()
        {
            UserManager user = (UserManager)Session["CurrentUser"];
            return DownloadFile(EncryptionHelper.Encrypt(user.FileName));
        }

    }
}