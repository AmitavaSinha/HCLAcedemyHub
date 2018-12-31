using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;
using System.ComponentModel.DataAnnotations;

namespace HCLAcademy.Models
{
    public class ErrorLogEntity : TableEntity
    {
        public ErrorLogEntity()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rowKey">Row key for entity</param>
        /// <param name="partitionKey">Partition key for entity</param>
        public ErrorLogEntity(int rowKey, string partitionKey, DateTime timeStamp,string message, string detailMessage, string applicationName, string userName, string source)
        {
            this.RowKey = rowKey.ToString();
            this.PartitionKey = partitionKey;
            this.Timestamp = timeStamp;
            this.Message = message;
            this.DetailMessage = detailMessage;
            this.ApplicationName = applicationName;
            this.UserName = userName;
            this.Source = source;

        }

        public string Message { get; set; }
        public string DetailMessage { get; set; }
        public string ApplicationName { get; set; }
        public string UserName { get; set; }
        public string Source { get; set; }

    }
}