using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Configuration;
using System.Web.Mvc;
using System.Web.Security;

namespace HCLAcademy.Controllers
{
    public class AuthorizeController : Controller
    {
        //
        // GET: /Authorize/
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            FetchAppConfig();
            return View();
        }

        [HttpGet]
        public ActionResult SiteMaintanance()
        {
            return View();
        }
        /// <summary>
        /// Check whether a user is authorized to Login to the website
        /// </summary>
        /// <param name="objAuth"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(SharePointAuthAutho objAuth)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    UserManager user = objAuth.Authorize(objAuth.UserName, objAuth.Password);

                    if (user != null && user.GroupPermission > 0)
                    {
                        Session.Add("IsOnline", user.IsOnline);

                        FormsAuthentication.SetAuthCookie(objAuth.DisplayName, objAuth.RememberMe);

                        if (Session["CurrentUser"] == null)
                        {
                            Session.Add("CurrentUser", user);
                        }
                        else
                        {
                            Session.Remove("CurrentUser");
                            Session.Add("CurrentUser", user);
                        }
                        return RedirectToAction("Home", "Home");
                    }

                    if (user != null && user.GroupPermission == 0)
                    {
                        ModelState.AddModelError("", "You are not Authorized to Log in");
                    }

                    if (user == null)
                    {
                        ModelState.AddModelError("", "Username and/or Password is incorrect");
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogToEventVwr(ex.Message, 0);
                if (ex.Message.Contains("denied"))
                {
                    ModelState.AddModelError("", "You are not Authorized to Log in");
                }
                else
                {
                    ModelState.AddModelError("", "Username and/or Password is incorrect");
                }

            }
            return View(objAuth);
        }
        /// <summary>
        /// Logout from the application
        /// </summary>
        /// <returns></returns>
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();

            Response.RemoveOutputCacheItem("/home/getlearningjourney");
            Response.RemoveOutputCacheItem("/home/getassessments");
            Response.RemoveOutputCacheItem("/wiki/wikipolicy");
            Response.RemoveOutputCacheItem("/wiki/wikidocumenttree");
            Response.RemoveOutputCacheItem("/onboard/onboarding");
            Response.RemoveOutputCacheItem("/training/training");

            // Removing session variables
            System.Web.HttpContext.Current.Session.Remove("StartAssessment");
            System.Web.HttpContext.Current.Session.Remove("UserSiteMenu");
            System.Web.HttpContext.Current.Session.Remove("BannersList");
            System.Web.HttpContext.Current.Session.Remove("CurrentUser");
            System.Web.HttpContext.Current.Session.Remove("LogoBase64Image");
            System.Web.HttpContext.Current.Session.Remove("Projects");
            System.Web.HttpContext.Current.Session.Remove("EditProject");
            System.Web.HttpContext.Current.Session.Remove("SPCredential");
            System.Web.HttpContext.Current.Session.Remove("AcademyConfig");
            System.Web.HttpContext.Current.Session.Remove("IsOnline");
            System.Web.HttpContext.Current.Session.Remove("UserRole");

            System.Web.HttpContext.Current.Session.Remove(AppConstant.AllVideoData);
            System.Web.HttpContext.Current.Session.Remove(AppConstant.AllWikiPolicyData);
            System.Web.HttpContext.Current.Session.Remove(AppConstant.AllSkillData);
            System.Web.HttpContext.Current.Session.Remove(AppConstant.AllEventsData);
            System.Web.HttpContext.Current.Session.Remove(AppConstant.AllCompetencyData);
            System.Web.HttpContext.Current.Session.Remove(AppConstant.AllTrainingData);
            System.Web.HttpContext.Current.Session.Remove(AppConstant.DefaultTrainingAssessmentData);
            System.Web.HttpContext.Current.Session.Remove(AppConstant.AllAssessmentData);
            System.Web.HttpContext.Current.Session.Remove(AppConstant.AllGEOData);

            return RedirectToAction("Login", "Authorize");
        }
        /// <summary>
        /// Fetches the configuration settings for the application
        /// </summary>
        public void FetchAppConfig()
        {
            Session["ClientName"] = null;
            if (ConfigurationManager.AppSettings["ClientName"] != null)
                Session["ClientName"] = ConfigurationManager.AppSettings["ClientName"].ToString();
        }
    }
}