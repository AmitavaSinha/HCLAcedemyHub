using HCLAcademy.Common;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HCLAcademy.Models
{
    public class LogEntity : TableEntity
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="partitionKey">It can have three values InformationLog,WarningLog or ErrorLog</param>
        /// <param name="userID">User information</param>
        public LogEntity(string partitionKey, string userID)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = userID;
            ApplicationName = AppConstant.ApplicationName;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="partitionKey">It can have three values InformationLog,WarningLog or ErrorLog</param>
        /// <param name="userID">User information</param>
        /// <param name="source">Module name where the log was generated</param>
        /// <param name="message">Information to be logged</param>
        /// <param name="detailMessage">Detail information to be logged</param>
        public LogEntity(string partitionKey, string userID, string applicationName, string source, string message, string detailMessage)
        {
            this.PartitionKey = partitionKey;
            this.UserName = userID;           
            this.ApplicationName = applicationName;
            this.Source = source;
            this.Message = message;
            this.DetailMessage = detailMessage;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="partitionKey">It can have three values InformationLog,WarningLog or ErrorLog</param>
        /// <param name="userID">User information</param>
        /// <param name="source">Module name where the log was generated</param>
        /// <param name="message">Information to be logged</param>
        public LogEntity(string partitionKey, string userID, string source, string message)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = userID;
            this.ApplicationName = AppConstant.ApplicationName;
            this.Source = source;
            this.Message = message;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        public LogEntity()
        {
            ApplicationName = AppConstant.ApplicationName;

        }

        /// <summary>
        /// Module name where the log was generated
        /// </summary>        
        public string Source { get; set; }
        /// <summary>
        /// Information to be logged///
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Detail information to be logged
        /// </summary>        
        public string DetailMessage { get; set; }
        /// <summary>
        /// Name of the application where log is generated
        /// </summary>
        public string ApplicationName { get; set; }
        /// <summary>
        /// User information
        /// </summary>
        public string UserName { get; set; }
    }

}