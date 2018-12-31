using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HCLAcademy.Controllers
{
    public class TrainingController : Controller
    {
        [Authorize]
        [SessionExpire]
        public ViewResult Training()
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<UserSkillDetail> traningModules = dal.GetUserTrainingsDetails(AppConstant.UserTrainingMapping);        //Get the list of Trainings assigned to users.
            return View(traningModules);
        }

        #region Static Trainings

        public ActionResult OPSTraining()
        {
            return View();
        }

        public ActionResult PolymerTraining()
        {
            return View();
        }

        public ActionResult ScalaTraining()
        {
            return View();
        }

        public ActionResult OracleApplicationExpressTraining()
        {
            return View();
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult TIBCOTraining()
        {
            return View();
        }

        public ActionResult FullStackDevelopmentTranning()
        {
            return View();
        }

        #endregion

        #region Training Materials 

        [Authorize]
        [SessionExpire]
        public ActionResult TrainingMaterials(string Selected)
        {
            ViewBag.Selected = (string.IsNullOrEmpty(Selected)) ? string.Empty : Selected.Replace(" ", "");
            ViewBag.Folder = (string.IsNullOrEmpty(Selected)) ? string.Empty : Selected.Replace(" ", "%20");
            return View();
        }

        [Authorize]
        [SessionExpire]
        public ActionResult TrainingDocumentTree(string folder)
        {
            WikiPolicyDocuments poldocs = new WikiPolicyDocuments();

            try
            {
                SharePointDAL spDal = new SharePointDAL();
                poldocs = spDal.GetWikiDocumentTree(Server, folder);
                return PartialView("_TrainingMaterials", poldocs.ListOfWikiDoc);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Training, TrainingDocumentTree", ex.Message, ex.StackTrace));
                throw;
            }
        }

        private List<WikiDocuments> GetChild(List<WikiDocuments> wikiDoc)
        {
            //Get child items
            List<WikiDocuments> wikiDocchild = new List<WikiDocuments>();
            foreach (WikiDocuments item in wikiDoc)
            {
                var wikichilddoc = from c in wikiDoc where c.DocumentURL.Equals(item.ParentFolderURL) select c;
                foreach (WikiDocuments itemwiki in wikichilddoc.ToList())
                {
                    if (itemwiki.WikiChild == null)
                    {
                        itemwiki.WikiChild = new List<WikiDocuments>();
                    }

                    itemwiki.WikiChild.Add(item);
                }
            }
            var d = from c in wikiDoc where c.ParentFolder.Equals("OnBoarding") select c;
            return d.ToList();
        }

        #endregion
    }
}