using Microsoft.WindowsAzure.Storage.Table;

namespace ConditionMatcherAzureFunction.Caching
{
    class StoredLuisResult : TableEntity
    {
        public StoredLuisResult(string userQuery, string conditionNameResult) : base(userQuery,userQuery)
        {

        }

        public StoredLuisResult() { }

       

        public string UserQuery { get; set; }
        public string ConditionResult { get; set; }

    }
}