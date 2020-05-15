using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ConditionMatcherAzureFunction.Caching
{
    class TableStorageClient
    {
        private readonly string _storageConnectionString;
        CloudTable _table;


        public TableStorageClient(string storageConnectionString)
        {
            _storageConnectionString = storageConnectionString;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            
            CloudTable table = tableClient.GetTableReference("luisresults");

            // Create the table if it doesn't exist.
            table.CreateIfNotExistsAsync().Wait();

            _table = table;


        }
        

        public async void AddRecordIfNotExists(string userQuery, string luisResult)
        {
            var tableEntity = new StoredLuisResult(userQuery,luisResult);
            var insertOperation = TableOperation.Insert(tableEntity);
            //  todo: check if this replaces
            await _table.ExecuteAsync(insertOperation);
        }

        public async Task<string> GetResultOrNull(string userQuery)
        {
            var retrieveOperation = TableOperation.Retrieve<StoredLuisResult>(null, userQuery, new List<string>() {"ConditionNameResult"});

            var result = await _table.ExecuteAsync(retrieveOperation);

            var luisResult = (StoredLuisResult) result?.Result;
            return luisResult?.ConditionResult;
        }

        public async void RemoveRecord(string userQuery)
        {
            var deleteOperation = TableOperation.Delete(new StoredLuisResult(userQuery, null));
            await _table.ExecuteAsync(deleteOperation);
        }

        





    }
}
