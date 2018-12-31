using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using HCLAcademy.Common;
using HCLAcademy.HtmlHelpers;
using HCLAcademy.Models;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using Colors = HCLAcademy.Models.Colors;

namespace HCL.Academy.Web.DAL
{
    public class Utilities
    {
        public static List<Status> GetAllStatus()
        {
            List<Status> statusList = new List<Status>();
            statusList.Add(new Status() { StatusId = 1, StatusName = "OnBoarded" });
            statusList.Add(new Status() { StatusId = 2, StatusName = "InProgress" });
            statusList.Add(new Status() { StatusId = 3, StatusName = "Selected" });
            statusList.Add(new Status() { StatusId = 3, StatusName = "Rejected" });

            return statusList;
        }

        public static List<BGVStatus> GetAllBGVStatus()
        {
            List<BGVStatus> bgvStatusList = new List<BGVStatus>();
            bgvStatusList.Add(new BGVStatus() { BGVStatusId = 1, BGVStatusName = "Initiated" });
            bgvStatusList.Add(new BGVStatus() { BGVStatusId = 2, BGVStatusName = "Rejected" });
            bgvStatusList.Add(new BGVStatus() { BGVStatusId = 3, BGVStatusName = "Completed" });

            return bgvStatusList;
        }

        public static List<ProfileSharing> GetAllProfileSharingStatus()
        {
            List<ProfileSharing> profileSharingList = new List<ProfileSharing>();
            profileSharingList.Add(new ProfileSharing() { ProfileSharingId = 1, ProfileSharingName = "Initiated" });
            profileSharingList.Add(new ProfileSharing() { ProfileSharingId = 2, ProfileSharingName = "Rejected" });
            profileSharingList.Add(new ProfileSharing() { ProfileSharingId = 3, ProfileSharingName = "Completed" });

            return profileSharingList;
        }

        public static string Truncate(string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + ".....";
        }

        public static void LogToEventVwr(string msg, int type)
        {

            try
            {
                ////For Azure
                if (RoleEnvironment.IsAvailable)
                {
                    if (type == 0)
                    {
                        Trace.TraceError(msg); // Write an error message   
                    }

                    if (type == 1)
                    {
                        Trace.TraceWarning(msg); // Write an warning message   
                    }

                    if (type == 2)
                    {
                        Trace.TraceInformation(msg); // Write an information message   
                    }
                }
                else
                {
                    EventLog eventLog1 = new EventLog();

                    if (!System.Diagnostics.EventLog.SourceExists("O365HCL"))
                        System.Diagnostics.EventLog.CreateEventSource("O365HCL", "O365HCLReportLog");

                    eventLog1.Source = "O365HCL";

                    // the event log source by which 

                    //the application is registered on the computer
                    eventLog1.Log = "O365HCLReportLog";

                    //type = 0 -- Error
                    if (type == 0)
                    {
                        eventLog1.WriteEntry(msg, EventLogEntryType.Error);
                    }

                    //type = 1 -- Warning
                    if (type == 1)
                    {
                        eventLog1.WriteEntry(msg, EventLogEntryType.Warning);
                    }

                    //type = 2 -- Information
                    if (type == 2)
                    {
                        eventLog1.WriteEntry(msg, EventLogEntryType.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Utilities, LogToEventVwr", ex.Message, ex.StackTrace));

                throw;
            }
        }

        public static string CreateBase64Image(Stream stream)
        {
            Image streamImage;

            /* Ensure we've streamed the document out correctly before we commit to the conversion */
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            /* Create a new image, saved as a scaled version of the original */
            streamImage = Image.FromStream(memoryStream);

            using (MemoryStream ms = new MemoryStream())
            {
                /* Convert this image back to a base64 string */
                streamImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static string GetSpreadsheetCellValue(SpreadsheetDocument doc, Cell cell)
        {
            string value = cell.CellValue.InnerText;
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return doc.WorkbookPart.SharedStringTablePart.SharedStringTable.ChildElements.GetItem(int.Parse(value)).InnerText;
            }
            return value;
        }

        public static Sheets GetAllWorksheets(SpreadsheetDocument document)
        {
            Sheets theSheets = null;
            WorkbookPart wbPart = document.WorkbookPart;
            theSheets = wbPart.Workbook.Sheets;
            return theSheets;
        }

        public static TraningStatus GetTraningStatus(bool IsCompleted, int NoOfAttempt, DateTime LastDayToComplete)
        {
            int maxAttempts = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxAttempts"]) == -1
                            ? int.MaxValue : Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxAttempts"]);
            // Inprogress, Completed,  overdue, Failed
            if (IsCompleted)
            {
                return TraningStatus.Completed;
            }
            else
            {
                if (NoOfAttempt >= maxAttempts)
                {
                    return TraningStatus.Failed;
                }
                else
                {
                    if (LastDayToComplete < DateTime.Now && NoOfAttempt < maxAttempts)
                    {
                        return TraningStatus.OverDue;
                    }
                    else //(training.LastDayToComplete >= DateTime.Now && training.NoOfAttempts < 3)
                    {
                        return TraningStatus.OnGoing;
                    }
                }

            }
        }

        public static OnboardingStatus GetOnBoardingStatus(bool IsCompleted, int NoOfAttempt, DateTime LastDayToComplete)
        {
            int maxAttempts = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxAttempts"]) == -1
                            ? int.MaxValue : Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxAttempts"]);
            // Inprogress, Completed,  overdue, Failed
            if (IsCompleted)
            {
                return OnboardingStatus.Completed;
            }
            else
            {
                if (NoOfAttempt >= maxAttempts)
                {
                    return OnboardingStatus.Failed;
                }
                else
                {
                    if (LastDayToComplete < DateTime.Now && NoOfAttempt < maxAttempts)
                    {
                        return OnboardingStatus.OverDue;
                    }
                    else //(training.LastDayToComplete >= DateTime.Now && training.NoOfAttempts < 3)
                    {
                        return OnboardingStatus.OnGoing;
                    }
                }

            }
        }

        public static Colors GetTrainingColor(TraningStatus tStatus)
        {
            Colors color = new Colors();

            switch (tStatus)
            {
                case TraningStatus.OnGoing:
                    color = Colors.blue;
                    break;

                case TraningStatus.Completed:
                    color = Colors.green;
                    break;

                case TraningStatus.OverDue:
                    color = Colors.red;

                    break;
                case TraningStatus.Failed:

                    color = Colors.red;

                    break;
                default:
                    color = Colors.blue;
                    break;
            }

            return color;
        }

        public static CourseStatus GetCourseStatus(List<UserTrainingDetail> tranings, List<UserAssessment> userAssessments)
        {

            CourseStatus CourseStstus = new CourseStatus();

            var match_Completed = tranings
                                 .FirstOrDefault(i => i.status.Equals(TraningStatus.Completed));

            var match_Ongoing = tranings
                                 .FirstOrDefault(i => i.status.Equals(TraningStatus.OnGoing));

            var match_Failed = tranings
                                 .FirstOrDefault(i => i.status.Equals(TraningStatus.Failed));

            var match_OverDue = tranings
                                 .FirstOrDefault(i => i.status.Equals(TraningStatus.OverDue));

            bool flag = false;
            if (userAssessments.Count == 0)
                flag = true;
            foreach(UserAssessment u in userAssessments)
            {
                if (u.IsAssessmentComplete)
                    flag = true;
                else 
                    flag = false;
            }

            if (match_Completed != null && (match_Ongoing == null && match_Failed == null && match_OverDue == null) && flag==true)
            {
                CourseStstus.cStatus = TraningStatus.Completed.GetDisplayName();
                CourseStstus.isDualStatus = false; 
                CourseStstus.bgColor = Colors.green; 
            }
            if (match_Completed != null && (match_Ongoing == null && match_Failed == null && match_OverDue == null) && flag == false)
            {
                CourseStstus.cStatus = TraningStatus.OnGoing.GetDisplayName();
                CourseStstus.isDualStatus = false;
                CourseStstus.bgColor = Colors.blue;
            }
            else if (match_Ongoing != null && match_Failed == null && match_OverDue == null)
            {
                CourseStstus.cStatus = TraningStatus.OnGoing.GetDisplayName(); 
                CourseStstus.isDualStatus = false; 
                CourseStstus.bgColor = Colors.blue;
            }
            //else if (match_Failed != null && match_Ongoing == null)
            //{
            //    CourseStstus.cStatus = TraningStatus.Failed.GetDisplayName(); //"Failed";
            //    CourseStstus.isDualStatus = false; // single color in UI
            //    CourseStstus.bgColor = Colors.red; //"red";
            //}
            //else if (match_OverDue != null && match_Ongoing == null)
            //{
            //    CourseStstus.cStatus = TraningStatus.OverDue.GetDisplayName(); // "Over Due";
            //    CourseStstus.isDualStatus = false; // single color in UI
            //    CourseStstus.bgColor = Colors.red;  //"red";

            //}

            //else if (match_Ongoing != null && match_Failed == null && match_OverDue == null)
            //{
            //    CourseStstus.cStatus = TraningStatus.OnGoing.GetDisplayName(); //"On Going";
            //    CourseStstus.isDualStatus = false; // single color in UI
            //    CourseStstus.bgColor = Colors.blue; //"blue";

            //}

            //else if (match_Ongoing != null && (match_Failed != null || match_OverDue != null))
            //{
            //    CourseStstus.cStatus = TraningStatus.OnGoing.GetDisplayName();
            //    CourseStstus.isDualStatus = true; // double color in UI
            //    CourseStstus.bgColor = Colors.combstatus; //"comb-status";

            //}

            return CourseStstus;
        }

    }
}