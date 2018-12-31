using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using HCLAcademy.Common;
using HCL.Academy.Web.Models;
using HCLAcademy.Models;

namespace HCL.Academy.Web.DAL
{
    public class AzureStorageTableOperations
    {
        CloudStorageAccount storageAccount;
        CloudTableClient tableClient;
        private string tableName;
        public AzureStorageTableOperations(StorageOperations operation)
        {
            try
            {
                var connStr = System.Configuration.ConfigurationManager.AppSettings["StorageConStr"].ToString();
                storageAccount = CloudStorageAccount.Parse(connStr);
                tableClient = storageAccount.CreateCloudTableClient();
                if(operation == StorageOperations.Logging)
                {
                    tableName = AppConstant.StorageTableLogs;
                    CloudTable table = tableClient.GetTableReference(tableName);
                    table.CreateIfNotExists();
                }
            }
            catch (Exception ex)
            {
                //LogHelper.AddLog(new LogEntity(Constants.PARTITION_ERRORLOG, "SYSTEM", ApplicationModules.COMMON_STORAGEDAL, ex.Message, ex.StackTrace));
            }
        }

        public void AddEntity(TableEntity entity, string partitionName)
        {
            try
            {
                CloudTable table;
                Guid g;
                g = Guid.NewGuid();
                entity.RowKey = g.ToString();
                entity.PartitionKey = partitionName;
                table = tableClient.GetTableReference(this.tableName);
                TableOperation insertOperation = TableOperation.Insert(entity);
                table.Execute(insertOperation);
            }
            catch (Exception ex)
            {
                UserManager user = (UserManager)HttpContext.Current.Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, user.EmailID.ToString(), AppConstant.ApplicationName, "Class:Method", ex.Message, ex.StackTrace));
            }
        }

        public void AddEntity(TableEntity entity)
        {
            //  try
            //  {
            CloudTable table;
            Random rnd = new Random();
            Guid g;
            g = Guid.NewGuid();
            entity.RowKey = g.ToString();
            table = tableClient.GetTableReference(this.tableName);
            TableOperation insertOperation = TableOperation.Insert(entity);
            table.Execute(insertOperation);
            //  }
            //   catch (Exception ex)
            //  {
            //       LogHelper.AddLog(new LogEntity(Constants.PARTITION_ERRORLOG, "SYSTEM", ApplicationModules.COMMON_STORAGEDAL, ex.Message, ex.StackTrace));
            //  }
        }

        public void EditEntity(TableEntity entity)
        {
            try
            {
                CloudTable table = tableClient.GetTableReference(tableName);
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);
                table.Execute(insertOrReplaceOperation);
            }
            catch (Exception ex)
            {
                //LogHelper.AddLog(new LogEntity(Constants.PARTITION_ERRORLOG, "SYSTEM", ApplicationModules.COMMON_STORAGEDAL, ex.Message, ex.StackTrace));
            }
        }

        public void DeleteEntity(TableEntity entity)
        {
            try
            {
                CloudTable table = tableClient.GetTableReference(tableName);
                TableOperation deleteOperation = TableOperation.Delete(entity);
                table.Execute(deleteOperation);
            }
            catch (Exception ex)
            {
                //LogHelper.AddLog(new LogEntity(Constants.PARTITION_ERRORLOG, "SYSTEM", ApplicationModules.COMMON_STORAGEDAL, ex.Message, ex.StackTrace));
            }
        }

        public List<T> GetEntities<T>(string partitionKey, Dictionary<string, string> propertyFilters) where T : TableEntity, new()
        {
            List<T> results = new List<T>();
            try
            {
                var table = tableClient.GetTableReference(this.tableName);
                var pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
                var combinedFilter = pkFilter;
                foreach (var properties in propertyFilters)
                {
                    var newFilter = TableQuery.GenerateFilterCondition(properties.Key, QueryComparisons.Equal, properties.Value);
                    combinedFilter = TableQuery.CombineFilters(combinedFilter, TableOperators.And, newFilter);
                }

                var query = new TableQuery<T>().Where(combinedFilter);
                results = table.ExecuteQuery(query).ToList();

            }
            catch (Exception ex)
            {
                //LogHelper.AddLog(new LogEntity(Constants.PARTITION_ERRORLOG, "SYSTEM", ApplicationModules.COMMON_STORAGEDAL, ex.Message, ex.StackTrace));

            }
            return results;
        }

        public List<T> GetEntities<T>(string partitionKey) where T : TableEntity, new()
        {
            List<T> results = new List<T>();
            try
            {
                var table = tableClient.GetTableReference(this.tableName);
                var pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
                var query = new TableQuery<T>().Where(pkFilter);
                results = table.ExecuteQuery(query).ToList();

            }
            catch (Exception ex)
            {
                //LogHelper.AddLog(new LogEntity(Constants.PARTITION_ERRORLOG, "SYSTEM", ApplicationModules.COMMON_STORAGEDAL, ex.Message, ex.StackTrace));

            }
            return results;
        }

        //public string GetStorageTable(string partitionKey)
        //{
        //    string tblName = string.Empty;

        //    /*if (partitionKey == Constants.PARTITION_ERRORLOG || partitionKey == Constants.PARTITION_INFORMATIONLOG || partitionKey == Constants.PARTITION_WARNINGLOG)
        //        tblName = Constants.LOGTABLENAME;
        //    else if (partitionKey == Constants.PARTITION_MULTIBRAININFOLOG || partitionKey == Constants.PARTITION_MULTIBRAINERRORLOG)
        //        tblName = Constants.MULTIBRAINLOGTABLE;
        //    else*/
        //        tblName = AppConstant.StorageTableName;

        //    return tblName;
        //}

        public object GetEntity<T>(string partitionKey, string rowKey) where T : TableEntity, new()
        {
            try
            {
                CloudTable table = tableClient.GetTableReference(this.tableName);
                TableOperation tableOperation = null;
                tableOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
                return table.Execute(tableOperation).Result;
            }
            catch (Exception ex)
            {
                //LogHelper.AddLog(new LogEntity(Constants.PARTITION_ERRORLOG, "SYSTEM", ApplicationModules.COMMON_STORAGEDAL, ex.Message, ex.StackTrace));
                return null;
            }
        }

        public List<T> GetEntities<T>(string strFilter, string strFilterValue) where T : TableEntity, new()
        {
            try
            {
                //string tblName = AppConstant.StorageTableName;
                //if (strFilter == "PartitionKey")
                //{
                //    tblName = GetStorageTable(strFilterValue);
                //}
                var table = tableClient.GetTableReference(tableName);
                var exQuery = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition(strFilter, QueryComparisons.Equal, strFilterValue));
                var results = table.ExecuteQuery(exQuery).Select(ent => (T)ent).ToList();
                return results;
            }
            catch (StorageException ex)
            {
                //LogHelper.AddLog(new LogEntity(Constants.PARTITION_ERRORLOG, "SYSTEM", ApplicationModules.COMMON_STORAGEDAL, ex.Message, ex.StackTrace));
                return null;
            }
        }

        public List<T> GetEntities<T>(string partitionValue, string strFilter, string strFilterValue) where T : TableEntity, new()
        {
            try
            {
                var table = tableClient.GetTableReference(tableName);
                var Q1 = new TableQuery<T>().Where(TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionValue),
                                                                                TableOperators.And,
                                                                                TableQuery.GenerateFilterCondition(strFilter, QueryComparisons.Equal, strFilterValue)));
                var results = table.ExecuteQuery(Q1).Select(ent => (T)ent).ToList();
                return results;

            }
            catch (StorageException ex)
            {
                //LogHelper.AddLog(new LogEntity(Constants.PARTITION_ERRORLOG, "SYSTEM", ApplicationModules.COMMON_STORAGEDAL, ex.Message, ex.StackTrace));
                return null;
            }
        }
    }
}