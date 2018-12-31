using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using HCL.Academy.Web.DAL;
using HCL.Academy.Web.Models;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace HCL.Academy.Web.Controllers
{
    public class ImportProjectsController : Controller
    {

        [Authorize]
        [SessionExpire]
        public ActionResult ImportProjects()
        {
            return View();
        }
        /// <summary>
        /// Upload the Project Data present in the file selected.
        /// </summary>
        /// <param name="uploadedFile"></param>
        /// <returns></returns>
        [Authorize]
        [SessionExpire]
        [HttpPost]
        public JsonResult UploadProjectDataFile(HttpPostedFileBase uploadedFile)
        {
            IDAL dal = (new DALFactory()).GetInstance();

            bool isProjectInserted = false;
            StringBuilder logText = new StringBuilder();
            try
            {
                logText.Append("<table border = '1'> <tr><th>Result</th></tr>");

                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    #region Read file data
                    if ((uploadedFile.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" || uploadedFile.ContentType == "application/octet-stream") &&
                        (uploadedFile.FileName.EndsWith(".xls") || uploadedFile.FileName.EndsWith(".xlsx")))
                    {

                        string url = ConfigurationManager.AppSettings["URL"].ToString();
                        List<Skill> allSkills = null;
                        List<Competence> allCompetencies = null;
                        List<Project> allProjects = null;

                    try
                    {
                        allSkills = dal.GetAllSkills();
                        allCompetencies = dal.GetAllCompetenceList();
                        allProjects = dal.GetAllProjects();
                    }
                    catch (Exception ex)
                    {
                        UserManager user = (UserManager)Session["CurrentUser"];
                        LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ImportProject,UploadProjectDataFile", ex.Message, ex.StackTrace));
                    }

                        List<DataTable> listDataTable = new List<DataTable>();
                        ProjectData objProjectData = new ProjectData();
                        using (SpreadsheetDocument doc = SpreadsheetDocument.Open(uploadedFile.InputStream, false))
                        {
                            Sheet sheet = doc.WorkbookPart.Workbook.Sheets.GetFirstChild<Sheet>();
                            var listOfSheets = Utilities.GetAllWorksheets(doc);
                            foreach (Sheet sheetItem in listOfSheets)
                            {
                                string sheetName = sheetItem.Name;
                                string sheetId = sheetItem.Id.Value;
                                Worksheet worksheet = (doc.WorkbookPart.GetPartById(sheetId) as WorksheetPart).Worksheet;
                                IEnumerable<Row> rows = worksheet.GetFirstChild<SheetData>().Descendants<Row>();
                                DataTable dt = new DataTable();
                                int rowcount = rows.Count();

                                foreach (Row row in rows)
                                {
                                    if (row != null)
                                    {
                                        if (row.RowIndex.Value == 1)
                                        {
                                            foreach (Cell cell in row.Descendants<Cell>())
                                            {
                                                dt.Columns.Add(Utilities.GetSpreadsheetCellValue(doc, cell));
                                            }
                                        }
                                        else
                                        {
                                            dt.Rows.Add();
                                            int i = 0;
                                            foreach (Cell cell in row.Descendants<Cell>())
                                            {
                                                dt.Rows[dt.Rows.Count - 1][i] = Utilities.GetSpreadsheetCellValue(doc, cell);
                                                i++;
                                            }
                                        }
                                    }
                                }
                                listDataTable.Add(dt);
                            }
                        }

                        objProjectData.projects = new List<Project>();
                        Project objProject = null;
                        foreach (DataRow item in listDataTable[0].Rows)
                        {
                            objProject = new Project();
                            objProject.ProjectName = Convert.ToString(item.ItemArray[0]);
                            List<Project> itemProject = allProjects.Where(project => (project.ProjectName).ToLower() == (objProject.ProjectName).ToLower()).ToList();
                            if (itemProject != null && itemProject.Count() > 0)
                            {
                                string duplicateProject = itemProject.FirstOrDefault().ProjectName;
                                logText.Append("<tr><td class='error'>The Project <span class='bold'>" + duplicateProject + "</span>already present.<td><tr>");
                            }
                            else
                            {
                                objProjectData.projects.Add(objProject);
                            }

                        }

                        objProjectData.projectSkills = new List<ProjectSkill>();
                        ProjectSkill objProjectSkill = null;
                        logText.Append("<tr><td>&nbsp;</td></tr>");
                        foreach (DataRow item in listDataTable[1].Rows)
                        {
                            objProjectSkill = new ProjectSkill();
                            objProjectSkill.Project = item.ItemArray[0] != null ? Convert.ToString(item.ItemArray[0]) : "";
                            objProjectSkill.Skill = item.ItemArray[1] != null ? Convert.ToString(item.ItemArray[1]) : "";

                            List<Skill> objSkill = allSkills.Where(i => i.SkillName == objProjectSkill.Skill).ToList();
                            objProjectSkill.SkillId = objSkill != null && objSkill.Count() > 0 ? Convert.ToInt32(objSkill.FirstOrDefault().SkillId) : -1;
                            if (objProjectSkill.SkillId == -1)
                            {
                                logText.Append("<tr><td class='error'>Project :<span class='bold'>" + objProjectSkill.Project + "</span>Skill : <span class='bold'>" + objProjectSkill.Skill + "</span> is not valid<td><tr>");

                            }
                            else
                            {
                                objProjectData.projectSkills.Add(objProjectSkill);
                            }

                        }

                        objProjectData.projectSkillResources = new List<ProjectSkillResource>();
                        ProjectSkillResource objProjectSkillResource = null;
                        logText.Append("<tr><td>&nbsp;</td></tr>");
                        foreach (DataRow item in listDataTable[2].Rows)
                        {
                            objProjectSkillResource = new ProjectSkillResource();
                            objProjectSkillResource.ProjectName = Convert.ToString(item.ItemArray[0]);
                            objProjectSkillResource.Skill = Convert.ToString(item.ItemArray[1]);
                            objProjectSkillResource.CompetencyLevel = Convert.ToString(item.ItemArray[2]);
                            objProjectSkillResource.ExpectedResourceCount = Convert.ToString(item.ItemArray[3]);
                            objProjectSkillResource.AvailableResourceCount = Convert.ToString(item.ItemArray[4]);

                            List<Competence> itemSkillResource = allCompetencies.Where(i => i.SkillName == objProjectSkillResource.Skill && i.CompetenceName.ToUpper() == objProjectSkillResource.CompetencyLevel.ToUpper()).ToList();
                            if (itemSkillResource != null && itemSkillResource.Count() > 0)
                            {
                                objProjectSkillResource.CompetencyLevelId = itemSkillResource != null && itemSkillResource.Count() > 0 ? itemSkillResource.FirstOrDefault().CompetenceId : -1;
                                objProjectSkillResource.SkillId = itemSkillResource != null && itemSkillResource.Count() > 0 ? itemSkillResource.FirstOrDefault().SkillId : -1;
                                objProjectData.projectSkillResources.Add(objProjectSkillResource);
                            }
                            else
                            {
                                logText.Append("<tr><td class='error'>" +
                                                    "For Project : <span class='bold'>" + objProjectSkillResource.ProjectName + "</span>" +
                                                    "Skill : <span class='bold'>" + objProjectSkillResource.Skill + "</span>" +
                                                    ", Competency Level :  <span class='bold'>" + objProjectSkillResource.CompetencyLevel + "</span>" +
                                                    " Combination doesn't exist. So it can't be added to ProjectSkillResource list<td><tr>"

                                               );
                            }


                        }
                        isProjectInserted = InsertProjectFromExcel(
                            objProjectData.projects, objProjectData.projectSkills, objProjectData.projectSkillResources, ref logText);
                    }
                    else
                    {
                        logText.Append("<tr><td>Please upload correct file format<td><tr>");
                    }
                    #endregion Read file data

                }
                else
                {
                    logText.Append("<tr><td>Please upload correct file format<td><tr>");
                }

                logText.Append("</table>");
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ImportProject,UploadProjectDataFile", ex.Message, ex.StackTrace));
            }
            return Json(new
            {
                statusCode = 200,
                status = isProjectInserted,
                message = logText.ToString(),
            }, JsonRequestBehavior.AllowGet);

        }
        /// <summary>
        /// Checks whether the Project Details were uploaded successfully.
        /// </summary>
        /// <param name="projects"></param>
        /// <param name="projectskills"></param>
        /// <param name="projectskillresources"></param>
        /// <param name="logText"></param>
        /// <returns></returns>
        private bool InsertProjectFromExcel(List<Project> projects, List<ProjectSkill> projectskills, 
            List<ProjectSkillResource> projectskillresources, ref StringBuilder logText)
        {
            IDAL dal = (new DALFactory()).GetInstance();

            bool result = false;
            try
            {
                List<Project> allProjects = null;
                List<ProjectSkill> allProjectSkills = null;
                List<ProjectSkillResource> allProjectSkillResource = null;

                foreach (Project project in projects)
                {
                    dal.AddProject(project.ProjectName);
                    logText.Append("<tr><td class='success'>The Project : <span class='bold'>" + project.ProjectName + "</span> added successfully to the list Projects.</td></tr>");
                }

                allProjects = dal.GetAllProjects();     //List of all Projects
                allProjectSkills = dal.GetAllProjectSkills();       //List of all Project Skills
                logText.Append("<tr><td>&nbsp;</td></tr>");
                foreach (ProjectSkill proskill in projectskills)
                {
                    List<ProjectSkill> objProSkill = allProjectSkills.Where(item => item.Project == proskill.Project && item.Skill == proskill.Skill).ToList();
                    if (objProSkill != null && objProSkill.Count > 0)
                    {
                        // project & skill combination already exists
                        logText.Append("<tr><td class='error'>Project :<span class='bold'>" + objProSkill.FirstOrDefault().Project + "</span>Skill : <span class='bold'>" + objProSkill.FirstOrDefault().Skill + "</span> is already present in ProjectSkill list.<td><tr>");

                    }
                    else // add project skill
                    {
                        List<Project> project = allProjects.Where(item => item.ProjectName == proskill.Project).ToList();
                        if (project != null && project.Count > 0)
                        {
                            dal.AddProjectSkill(
                                allProjects.Where(item => item.ProjectName == proskill.Project).FirstOrDefault().ID, 
                                proskill.SkillId);

                            logText.Append("<tr><td class='success'>Project : <span class='bold'>" + proskill.Project + "</span> and Skill : <span class='bold'>" + proskill.Skill + "</span> added successfully to the list ProjectSkills.</td></tr>");
                        }
                        else
                        {
                            // project doesn't exist in Project list
                            // So it can't be added to project skill
                            logText.Append("<tr><td class='error'>Project :<span class='bold'>" + proskill.Project + "</span>is not available in Project list so It can't be added to ProjectSkill<td><tr>");
                        }
                    }
                }

                allProjectSkillResource = dal.GetAllProjectSkillResources();
                logText.Append("<tr><td>&nbsp;</td></tr>");
                foreach (ProjectSkillResource skillResource in projectskillresources)
                {
                    List<Project> project = allProjects.Where(item => item.ProjectName == skillResource.ProjectName).ToList();
                    if (project != null && project.Count > 0)
                    {
                        int projectid = project != null && project.Count() > 0 ? project.FirstOrDefault().ID : -1;
                        List<ProjectSkillResource> objProSkillResource = allProjectSkillResource.Where(item => item.ProjectId == projectid && item.SkillId == skillResource.SkillId && item.CompetencyLevelId == skillResource.CompetencyLevelId).ToList();
                        if (objProSkillResource != null && objProSkillResource.Count() > 0)
                        {
                            // Item already exist
                            // Can't be added again
                            logText.Append("<tr><td class='error'>Project :<span class='bold'>" + skillResource.ProjectName + "</span> Skill : <span class='bold'>" + skillResource.Skill + "</span>, Competency Level : <span class='bold'>" + skillResource.CompetencyLevel + "</span> already exists in ProjectSkillResource list.<td><tr>");
                        }
                        else
                        {
                            dal.AddProjectSkillResource(allProjects.Where(item => item.ProjectName == skillResource.ProjectName).FirstOrDefault().ID,
                                skillResource);
                            logText.Append("<tr><td class='success'>Project : " + skillResource.ProjectName + " Skill : " + skillResource.Skill + " and Competency Level : " + skillResource.CompetencyLevel + " added successfully to the list ProjectSkillResource. </td></tr>");
                        }
                    }
                    else
                    {
                        // Can't be added
                        // Project doesn't exist
                        logText.Append("<tr><td class='error'>Project is not available for combination, Project :<span class='bold'>" + skillResource.ProjectName + "</span> Skill : <span class='bold'>" + skillResource.Skill + "</span>, Competency Level : <span class='bold'>" + skillResource.CompetencyLevel + "</span>. So it can't be added to ProjectSkillResource<td><tr>");
                    }
                }
                result = true;
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "ImportProject, InsertProjectFromExcel", ex.Message, ex.StackTrace));

                Utilities.LogToEventVwr(ex.Message, 0);
                result = false;
            }
            return result;
        }
    }
}