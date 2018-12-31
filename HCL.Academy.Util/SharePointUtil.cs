using HCLAcademy.Common;
using Microsoft.SharePoint.Client;
using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace HCLAcademy.Util
{
    public class SharePointUtil
    {
        #region EmailQueue

        public static bool AddToEmailQueue(ClientContext context, string TemplateCode, Hashtable DynamicKeyValues,
            string RecipientTo, string RecipientCc, Web subSite = null)
        {
            string emailSub = string.Empty;
            string emailBody = string.Empty;
            List lstDoc = null;

            if (subSite == null)
            {
                lstDoc = context.Web.Lists.GetByTitle(AppConstant.EmailTemplate);                
            }
            else
            {
                lstDoc = subSite.Lists.GetByTitle(AppConstant.EmailTemplate);               
            }

            string camlQuery = "<View><Query><Where><Eq><FieldRef Name='Title'/><Value Type='Text'>" + TemplateCode + "</Value></Eq></Where></Query></View>";
            string emailSubjectColumnName = string.Empty;

            ListItemCollection lstItemsDoc = GetListItems(AppConstant.EmailTemplate, context, null, camlQuery, subSite);

            if (lstDoc.ContainsField("EmailSubject"))
            {
                emailSubjectColumnName = "EmailSubject";
            }
            else
            {
                emailSubjectColumnName = "EmailSubject1";
            }

            if (lstItemsDoc != null & lstItemsDoc.Count() > 0)
            {
                try
                {
                    foreach (ListItem item in lstItemsDoc)
                    {                        
                        emailSub = Convert.ToString(item[emailSubjectColumnName]);
                        emailBody = Convert.ToString(item["EmailBody1"]);

                        foreach (string key in DynamicKeyValues.Keys)
                        {
                            emailSub = emailSub.Replace("[##" + key + "##]", Convert.ToString(DynamicKeyValues[key]));
                            emailBody = emailBody.Replace("[##" + key + "##]", Convert.ToString(DynamicKeyValues[key]));
                        }
                        break;
                    }
                }
                catch(Exception)
                {
                    throw;
                }
            }

            bool status = false;
            
            string clientName = ConfigurationManager.AppSettings["ClientName"].ToString();
            
            SendMailRequest objtb = new SendMailRequest();
            objtb.To = RecipientTo;
            objtb.Cc = RecipientCc;
            objtb.SenderEmailId = "no-reply@hcl.com";
            objtb.SenderName = "HCL Academy";
            if (!string.IsNullOrEmpty(clientName))
            {
                objtb.SenderName = clientName + " Academy";
            }
            objtb.Subject = emailSub;
            objtb.Body = emailBody;

            Task.Factory.StartNew(() => EmailHelper.SendEmail(objtb));

            status = true;
            return status;
        }

        #endregion

        #region Get SP List Items
        public static ListItemCollection GetListItems(string lstGetName, ClientContext context, string webName, string camlQuery, Web subSite=null)
        {   
            try
            {
                CamlQuery query = new CamlQuery();
                Web web = null;
                if (!string.IsNullOrEmpty(webName))
                {                    
                    if (subSite == null)
                    {
                        web = context.Site.OpenWeb(webName);
                    }
                    else
                    {
                        web = subSite;
                    }                
                    List lst = web.Lists.GetByTitle(lstGetName);                
                    context.Load(lst, l => l.Fields);
                    
                    if (string.IsNullOrEmpty(camlQuery))
                    {
                        query = CamlQuery.CreateAllItemsQuery();
                    }
                    else
                    {
                        query.ViewXml = camlQuery;
                    }

                    ListItemCollection items = lst.GetItems(query);
                    context.Load(items, itemsc => itemsc.Include(item => item.FieldValues));
                    context.ExecuteQuery();
                    return items;
                }
                else
                {
                    if (subSite == null)
                    {
                        web = context.Web;
                    }
                    else
                    {
                        web = subSite;
                    }
                    
                    List lst = web.Lists.GetByTitle(lstGetName);                    
                    context.Load(lst, l => l.Fields);
                    
                    if (string.IsNullOrEmpty(camlQuery))
                    {
                        query = CamlQuery.CreateAllItemsQuery();
                    }
                    else
                    {
                        query.ViewXml = camlQuery;
                    }
                    ListItemCollection items = lst.GetItems(query);                    
                    context.Load(items);
                    context.ExecuteQuery();                    
                    return items;
                }  
            }
            catch (Exception ex)
            {                
                throw;
            }

        }
        #endregion

        public static ListItemCollection GetListItems(string lstGetName,string webName, string camlQuery, Web subSite = null)
        {
            //Console.WriteLine("GetListItems started");
            try
            {
                string url = ConfigurationManager.AppSettings["URL"].ToString();
                using (ClientContext context = new ClientContext(url))
                {
                    context.Credentials = (ICredentials)HttpContext.Current.Session["SPCredential"];
                    CamlQuery query = new CamlQuery();

                    Web web = null;
                    if (!string.IsNullOrEmpty(webName))
                    {
                        if (subSite == null)
                        {
                            web = context.Site.OpenWeb(webName);
                        }
                        else
                        {
                            web = subSite;
                        }
                        // context.Load(web, w => w.Lists);
                        List lst = web.Lists.GetByTitle(lstGetName);
                        //context.Load(lst);
                        context.Load(lst, l => l.Fields);
                        // context.ExecuteQuery();
                        if (string.IsNullOrEmpty(camlQuery))
                        {
                            query = CamlQuery.CreateAllItemsQuery();
                        }
                        else
                        {
                            query.ViewXml = camlQuery;
                        }


                        ListItemCollection items = lst.GetItems(query);

                        // Retrieve all items in the ListItemCollection from List.GetItems(Query). 
                        context.Load(items, itemsc => itemsc.Include(item => item.FieldValues));
                        context.ExecuteQuery();
                        return items;
                    }
                    else
                    {
                        if (subSite == null)
                        {
                            web = context.Web;
                        }
                        else
                        {
                            web = subSite;
                        }
                        //context.Load(web, w => w.Lists);
                        List lst = web.Lists.GetByTitle(lstGetName);
                        //context.Load(lst);
                        context.Load(lst, l => l.Fields);
                        //context.ExecuteQuery();
                        if (string.IsNullOrEmpty(camlQuery))
                        {
                            query = CamlQuery.CreateAllItemsQuery();
                        }
                        else
                        {
                            query.ViewXml = camlQuery;
                        }
                        ListItemCollection items = lst.GetItems(query);

                        // Retrieve all items in the ListItemCollection from List.GetItems(Query). 
                        context.Load(items);
                        context.ExecuteQuery();
                        //Console.WriteLine("GetListItems ended");
                        return items;

                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public static bool DeteleItemById(ClientContext context, int id, string listName)
        {
            bool result = false;
            try
            {
                List slist = context.Web.Lists.GetByTitle(listName);
                slist.GetItemById(id).DeleteObject();

                context.ExecuteQuery();
            }
            catch (Exception)
            {

            }
            return result;
        }

        public static Stream GetFile(string itemPath, ClientContext context)
        {
            try
            {
                /*
                 Logic explained in example below:-
                 itemPath = "http://sp201601/sites/academy/SiteAssets/logo/logo.png"
                 the web url i.e. siteurl = "http://sp201601/sites/academy"
                 the relative url i.e. siterelativeurl= "/sites/academy"

                 */
                Web web = context.Web;
                context.Load(web, w => w.Url, w => w.ServerRelativeUrl);
                context.ExecuteQuery();
                string siteurl = web.Url;
                string siterelativeurl = web.ServerRelativeUrl;

                string url1 = siteurl.Substring(0, siteurl.LastIndexOf(siterelativeurl));

                itemPath = itemPath.Substring(url1.Length);

                Microsoft.SharePoint.Client.File oFile = web.GetFileByServerRelativeUrl(itemPath);
                context.Load(oFile);
                ClientResult<Stream> stream = oFile.OpenBinaryStream();
                context.ExecuteQuery();
                return stream.Value;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }

    public static class FieldCollectionExtensions
    {
        public static bool ContainsField(this List list, string fieldName)
        {
            var context = list.Context;
            var result = context.LoadQuery(list.Fields.Where(f => f.InternalName == fieldName));
            context.ExecuteQuery();
            return result.Any();
        }
    }
}