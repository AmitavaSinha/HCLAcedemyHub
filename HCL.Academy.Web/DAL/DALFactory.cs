using System.Configuration;

namespace HCL.Academy.Web.DAL
{
    public class DALFactory
    {
        private IDAL dal;

        public DALFactory()
        {
            string dataStore = ConfigurationManager.AppSettings["DATASTORE"].ToString();

            switch (dataStore)
            {
                case "SharePoint":
                    dal = new SharePointDAL();
                    break;
                case "SqlSvr":
                    dal = new SqlSvrDAL();
                    break;
            }
        }

        /// <summary>
        /// DataStore = SharePoint / SqlSvr
        /// </summary>
        /// <param name="DataStore"></param>
        public DALFactory(string DataStore)
        {
            switch (DataStore)
            {
                case "SharePoint":
                    dal = new SharePointDAL();
                    break;
                case "SqlSvr":
                    dal = new SqlSvrDAL();
                    break;
            }
        }

            public IDAL GetInstance()
        {
            return dal;
        }
    }
}