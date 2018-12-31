using HCL.Academy.Web.DAL;
using HCLAcademy.Models;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.UI;

namespace HCLAcademy.Controllers
{
    public class CareerProgressionController : Controller
    {
        /// <summary>
        /// Get the skills of the logged in user
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [OutputCache(Duration = 600, VaryByCustom = "User", VaryByParam = "", Location = OutputCacheLocation.Server, NoStore = false)]
        public ActionResult Career()
        {
            IDAL dal = (new DALFactory()).GetInstance();
            List<UserSkill> lstSkills = dal.GetUserSkillsOfCurrentUser();            
            return View(lstSkills);
        }
    }
}