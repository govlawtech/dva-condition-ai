using Microsoft.WindowsAzure.Storage;

namespace ConditionMatcherAzureFunction.Caching
{
    class LuisAnswerCache
    {
        private readonly string _azureStorageConnectionString;
        private CloudStorageAccount _cloudStorageAccount;

        public LuisAnswerCache(string azureStorageConnectionString)
        {
            _azureStorageConnectionString = azureStorageConnectionString;
        }


        private void Init() {

            
            if (CloudStorageAccount.TryParse(_azureStorageConnectionString, out _cloudStorageAccount))
            {
                // If the connection string is valid, proceed with operations against Blob storage here.
                
            }
            else
            {
                // Otherwise, let the user know that they need to define the environment variable.
             
            }
        }

        // get storage key from env settings
    }
}
