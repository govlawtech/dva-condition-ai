using System.Net.Http;

namespace ConditionMatcherAzureFunction.ExternalServices
{
    public class CachingLuisRestClient : LuisRestClient
    {
        public CachingLuisRestClient(string luisAppId, string luisSubscriptionKey, string bingSpellCheckKey, HttpClient httpClient) : base(luisAppId, luisSubscriptionKey, bingSpellCheckKey, httpClient)
        {
        }

//        var cachedResult = await tableStorageClient.GetResultOrNull(userDescription);
//            if (cachedResult != null)
//        {
//            var cachedResponse = JArray.Parse(cachedResult);
//            // check that all conditions are still valid
//            // remove from cache if not
//            var conditionNames = from jobj in cachedResponse.AsJEnumerable()
//                let conditionName = jobj["currentInForceSopConditionName"]
//                select conditionName.Value<string>();

//            var allConditionNamesStillValid = conditionNames
//                .All(n => _fullConditionNames.Contains(n));

//            if (!allConditionNamesStillValid) // cache stale
//            {
//                tableStorageClient.RemoveRecord(userDescription);
//            }
//            else // cache result good
//            {
//                var
//            }
//        }
//        else // cache miss
//        {
//            conditionMatches = await CreateLuisResponseData(luisClientSettings, userDescription, topValue, confidenceAsInt);

//// add to cache
//            tableStorageClient.AddRecordIfNotExists(userDescription, conditionMatches.ToString());
                        
//        }
    }
}