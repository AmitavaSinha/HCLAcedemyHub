using HCL.Academy.Web.Models;
using HCLAcademy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HCL.Academy.Web.DAL
{
    public class LogHelper
    {
        /// <summary>
        /// This method adds any exception,information,warning to Azure storage
        /// </summary>
        /// <param name="e">Logentity</param>
        public static void AddLog(LogEntity e)
        {
            AzureStorageTableOperations tblOps = new AzureStorageTableOperations(StorageOperations.Logging);
            tblOps.AddEntity(e);
        }
    }
}