using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using HCLAcademy.Utility;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.UI;

namespace HCLAcademy.Controllers
{
    public class WikiController : Controller
    {
        // GET: Wiki
        [Authorize]
        [SessionExpire]
        public ActionResult Wiki(string Selected)
        {
            ViewBag.Selected = (string.IsNullOrEmpty(Selected)) ? Selected : Selected.Replace(" ", "");
            return View();
        }

        /// <summary>
        /// Fetches all the Policy Documents.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByCustom = "User", VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]

        public ActionResult WikiPolicy()
        {
            try
            {
                //Get Policy Items
                SharePointDAL dal = new SharePointDAL();
                WikiPolicyDocuments poldocs = dal.GetWikiPolicyDocuments();
                return View(poldocs);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Wiki, WikiPolicy", ex.Message, ex.StackTrace));
                throw;
            }
        }

        /// <summary>
        /// Download the Wiki file from path specified.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        public FileResult DownloadWikiFile(string filePath)
        {
            string decryptFileName = EncryptionHelper.Decrypt(filePath);        //Decrypt the File Name
            string fileName = decryptFileName.Substring(decryptFileName.LastIndexOf('/') + 1);

            SharePointDAL dal = new SharePointDAL();

            System.IO.Stream fileBytes = dal.DownloadDocument(decryptFileName);     //Download the selected document.

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }
        /// <summary>
        /// Fetches the list of Wiki Documents.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByCustom = "User", VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public ActionResult WikiDocumentTree()
        {
            SharePointDAL dal = new SharePointDAL();
            List<WikiDocuments> listOfWikiDoc = dal.GetWikiDocumentTree(Server);
            return PartialView("_WikiDocumentTree", listOfWikiDoc);
        }

    }
}