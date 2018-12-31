using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Net;
using System.Security;
using System.Web;
using HCL.Academy.Web.DAL;
using Microsoft.SharePoint.Client;

namespace HCLAcademy.Models
{
    public class SharePointAuthAutho
    {
        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        public string DisplayName { get; set; }

        public ClientContext ClientContext { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember on this computer")]
        public bool RememberMe { get; set; }


        Web web = null;
        string url = string.Empty;

        #region SP 2013 Online credential
        /// <summary>
        /// Get SP 2013 Online Credentials
        /// </summary>
        /// <returns></returns>
        private static SharePointOnlineCredentials GetSpOnlineCredential(Uri webUri, string userName, string password)
        {
            try
            {
                var securePassword = new SecureString();
                foreach (var ch in password) securePassword.AppendChar(ch);

                return new SharePointOnlineCredentials(userName, securePassword);
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region Authorize the User
        /// <summary>
        /// Get SP 2013 Online User Details
        /// </summary>
        /// <returns></returns>
        public UserManager Authorize(string userName, string password)
        {
            UserManager user = new UserManager();

            try
            {

                string url = ConfigurationManager.AppSettings["URL"].ToString();
                Uri uri = new Uri(url, UriKind.Absolute);
                var isOnline = false;                
                string SharepointPlatform = ConfigurationManager.AppSettings["SharepointPlatform"].ToString();

                if (SharepointPlatform == "SPOnline")
                {
                    isOnline = true;
                }

                user.IsOnline = isOnline;

                try
                {
                    //Get User Groups
                    string spReaderGroup = ConfigurationManager.AppSettings["AcademyReaderGroup"].ToString();
                    string spMemberGroup = ConfigurationManager.AppSettings["AcademyMemberGroup"].ToString();
                    string spOwnerGroup = ConfigurationManager.AppSettings["AcademyOwnerGroup"].ToString();
                    string spPmoGroup = ConfigurationManager.AppSettings["AcademyPMO"].ToString();

                    //Get Client Context
                    using (ClientContext clientContext = new ClientContext(url))
                    {
                        if (isOnline)
                        {
                            var credential = GetSpOnlineCredential(uri, userName, password);
                            clientContext.Credentials = credential;
                        }
                        else
                        {
                            NetworkCredential credential = new NetworkCredential(userName, password);
                            clientContext.Credentials = credential;
                        }

                        web = clientContext.Web;
                        clientContext.Load(web, w => w.Url,
                                                w => w.SiteGroups,
                                                w => w.CurrentUser,
                                                w => w.CurrentUser.Groups);
                        clientContext.Load(web.CurrentUser);
                        clientContext.ExecuteQuery();

                        //Assign values to custom object
                        user.SPUserId = web.CurrentUser.Id;
                        user.EmailID = web.CurrentUser.Email;
                        user.UserName = web.CurrentUser.Title;
                        DisplayName = web.CurrentUser.Title;
                        user.isSiteAdmin = web.CurrentUser.IsSiteAdmin;

                        //Groups
                        List<string> grp = new List<string>();

                        foreach (Group gp in web.CurrentUser.Groups)
                        {
                            grp.Add(gp.Title);
                        }

                        int groupValidation = 0;

                        //Reader Group
                        if (grp.Contains(spReaderGroup))
                        {
                            groupValidation = 1;
                            user.Groups = grp;
                            user.GroupPermission = groupValidation;
                        }

                        //Members Group
                        if (grp.Contains(spMemberGroup))
                        {
                            groupValidation = 2;
                            user.Groups = grp;
                            user.GroupPermission = groupValidation;
                        }

                        //Owners Group
                        if (grp.Contains(spOwnerGroup))
                        {
                            groupValidation = 3;
                            user.Groups = grp;
                            user.GroupPermission = groupValidation;
                        }

                        //Owners Group
                        if (grp.Contains(spPmoGroup))
                        {
                            groupValidation = 4;
                            user.Groups = grp;
                            user.GroupPermission = groupValidation;
                        }

                        if (groupValidation == 0)
                        {
                            user.Groups = null;
                            user.GroupPermission = groupValidation;
                        }
                        ClientContext = clientContext;
                        string dataStore = ConfigurationManager.AppSettings["DATASTORE"].ToString();
                        if(dataStore== "SqlSvr")
                        {
                            SqlSvrDAL dal = new SqlSvrDAL();
                            user.DBUserId = dal.GetUserId(web.CurrentUser.Email);
                        }
                    }
                }
                catch (Exception ex)
                {
                    return user;
                }

                HttpContext.Current.Session["SPCredential"] = ClientContext.Credentials;
                 
                return user;
            }
            catch (Exception)
            {
                user = null;
                return user;
            }
        }
        #endregion
    }
}