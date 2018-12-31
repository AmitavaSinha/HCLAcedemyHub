using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace HCLAcademy.Controllers
{
    public class AcademyVideoController : Controller
    {

        // GET: Get All Videos
        [Authorize]
        [SessionExpire]
        public ActionResult Index()
        {
            try
            {
                //IDAL dal = (new DALFactory()).GetInstance();
                SharePointDAL dal = new SharePointDAL();
                List<AcademyVideo> lstAcademyVideo = dal.GetAllAcademyVideos();
                return View(lstAcademyVideo);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "AcademyVideo, Index", ex.Message, ex.StackTrace));

                throw;
            }
        }

        //POST: Ajax Call
        [HttpPost]
        [Authorize]
        [SessionExpire]
        public PartialViewResult GetVideoWindow(string url, string videoTitle)
        {
            AcademyVideo av = new AcademyVideo();
            av.Url = url;
            av.Title = videoTitle;
            return PartialView("_AcademyVideoModel", av);
        }

        //url to stream
        [Authorize]
        [SessionExpire]
        public FileStreamResult GetVideoStream(string url)
        {
            SharePointDAL dal = new SharePointDAL();            
            return dal.GetVideoStream(url);
        }

    }
}
