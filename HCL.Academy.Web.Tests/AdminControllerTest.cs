using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using HCLAcademy.Models;
using HCLAcademy.Controllers;
using HCL.Academy.Web.DAL;
using System.Web.Mvc;
using Moq;


namespace HCL.Academy.Web.Tests
{
    [TestFixture]
    public class AdminControllerTest
    {

        Mock<IDAL> userDAL;
        
        [SetUp]
        public void SetUp()
        {
            userDAL = new Mock<IDAL>();
        }

        [Test]
        public void IndexTest()
        {
            UserOnBoarding objUserOnBoarding = new UserOnBoarding();
            //var obj = new AdminController(dal);
            List<Skill> skills = new List<Skill>()
            {
                new Skill { SkillId = 1, SkillName = "C# Unleashed" },
                new Skill { SkillId = 2, SkillName = "ASP.Net Unleashed" },
                new Skill { SkillId = 3, SkillName = "Silverlight Unleashed" }
            };

            userDAL.Setup(x => x.GetAllSkills()).Returns(skills);
            var Res = userDAL.Object.GetAllSkills();
            //var actResult = obj.OnBoardAdmin() as ViewResult;
            //Assert.AreEqual(actResult.ViewName, "OnBoardAdmin");
        }
    }
}
