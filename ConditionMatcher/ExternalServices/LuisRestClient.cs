using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ConditionMatcherAzureFunction.ExternalServices
{

    public class LuisRestClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _url;


        public LuisRestClient(string luisAppId, string luisSubscriptionKey, string bingSpellCheckKey, HttpClient httpClient)
        {
            _httpClient = httpClient;


            // https://australiaeast.api.cognitive.microsoft.com/luis/v2.0/apps/e7d36bc1-18ee-479c-8b73-f6baecc23e81?subscription-key=2981afa0e50e4679935a02c7624fd26e&spellCheck=true&bing-spell-check-subscription-key={YOUR_BING_KEY_HERE}&verbose=true&timezoneOffset=0&q=

            _url =
                $"https://australiaeast.api.cognitive.microsoft.com/luis/v2.0/apps/{luisAppId}?subscription-key={luisSubscriptionKey}&spellCheck=true&bing-spell-check-subscription-key={bingSpellCheckKey}&verbose=true&timezoneOffset=0&q=";
        }


        public async Task<IList<Tuple<string, int>>> GetMatchingIntents(string query)
        {
          
                var getRequest = $"{_url}{query}";
                var req = new HttpRequestMessage(HttpMethod.Get, getRequest);
            var resp = (await _httpClient.SendAsync(req)).EnsureSuccessStatusCode();

            var body = await  resp.Content.ReadAsStringAsync();
                var results = ExtractIntents(body);
                return results;
            
        }

        private static IList<Tuple<string, int>> ExtractIntents(string response)
        {
            var jObject = JObject.Parse(response);
            var results = from intent in jObject["intents"]
                          let intentName = intent["intent"].ToString()
                          let score = Double.Parse(intent["score"].ToString())
                          let confidencePercentage = Convert.ToInt16(Math.Round(score * 100, 0))
                          select new Tuple<string, int>(intentName, confidencePercentage);

            return results.ToList();
        }
    }
}