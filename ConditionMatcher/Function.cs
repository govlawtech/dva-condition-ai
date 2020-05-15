using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using ConditionMatcherAzureFunction.Caching;
using ConditionMatcherAzureFunction.ExternalServices;
using ConditionMatcherAzureFunction.Implementations;
using ConditionMatcherAzureFunction.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace ConditionMatcherAzureFunction
{
    public static class ConditionMatcherFunction
    {
        private static HttpClient _httpClient = new HttpClient() {Timeout = new TimeSpan(0, 0, 5)};

        private static TelemetryClient telemetry = new TelemetryClient(
            new TelemetryConfiguration()
            {
                InstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")
            });        

        private static Dictionary<string, string> _conditionNamesLoweredToConditionName = Utils
            .DeserialiseConditionsFromFileSnapshot(ConditionMatcher.Properties.Resources.conditionsJson)
            .ToDictionary(s => s.Name.ToLowerInvariant(), s => s.Name);

        private static IMultipleConditionIdentifier _multipleConditionIdentifier = new MultipleConditionIdentifier();

        [FunctionName("ConditionMatcher")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "suggestSoPCondition")]
            HttpRequest req, ILogger log, ExecutionContext context)
        {
            telemetry.Context.Operation.Id = context.InvocationId.ToString();
            telemetry.Context.Operation.Name = "condition-match-request";
            
            var correctAcceptHeaderInPlace = req.GetTypedHeaders().Accept
                .Contains(MediaTypeHeaderValue.Parse("application/json; charset=utf-8"));
            
            //https://www.owasp.org/index.php/REST_Security_Cheat_Sheet
            if (!correctAcceptHeaderInPlace)
            {
                return new BadRequestErrorMessageResult("Need Accept header: 'application/json; charset=utf-8'");
            }

            string apiVersion = "v1";
            StringValues apiVersionHeaderValue;
            req.Headers.TryGetValue("version", out apiVersionHeaderValue);
            var versionHeader = apiVersionHeaderValue.FirstOrDefault();
            if (versionHeader != null)
            {
                if (versionHeader != "v2" && versionHeader != "v1")
                {
                    return new BadRequestErrorMessageResult("Unrecognised version header value.  Allowed: 'v1','v2'.");
                }

                apiVersion = versionHeader;
            }

            var environmentVariables = Environment.GetEnvironmentVariables();
            var luisClientSettings = new[] {"LuisAppId", "LuisSubscriptionKey", "BingSpellCheckApiKey"}
                .ToDictionary(s => s, s => environmentVariables.Contains(s) ? environmentVariables[s] : null);

            var missingEnvironmentVars = luisClientSettings.Where(i => i.Value == null);
            if (missingEnvironmentVars.Any())
            {
                log.LogError("Missing environment variables: " +
                          String.Join(",", missingEnvironmentVars.Select(i => i.Value)));
                return new InternalServerErrorResult();
            }

            var queryParamsResult = ParseQueryParams(req);
            if (queryParamsResult.IsSuccess == false)
            {
                return new BadRequestErrorMessageResult(queryParamsResult.ErrMsg);
            }
            var userDescription = queryParamsResult.UserDescription;
            var topValue = queryParamsResult.NumberOfConditionsToReturn;
            var confidenceAsInt = queryParamsResult.ConfidenceThreshold;
            
            IList<Tuple<string, int>> conditionMatchData = new List<Tuple<string, int>>();
            bool luisCalled = false;
            try
            {
                // check if condition matches name
                var inputTrimmedAndLowered = ((string) userDescription).ToLowerInvariant().Trim();

                telemetry.TrackEvent("user-query", new Dictionary<string, string>() { {"userQuery",inputTrimmedAndLowered}});

                if (_conditionNamesLoweredToConditionName.ContainsKey(inputTrimmedAndLowered))
                {
                    IList<Tuple<string, int>> match = new List<Tuple<string, int>>()
                    {
                        new Tuple<string, int>(_conditionNamesLoweredToConditionName[inputTrimmedAndLowered], 100)
                    };

                    telemetry.TrackEvent("string-match", new Dictionary<string, string>()  {{"condition",_conditionNamesLoweredToConditionName[inputTrimmedAndLowered]}});

                    conditionMatchData = match;
                }
                else // call LUIS
                {
                    var luisClient = new LuisRestClient(
                        luisClientSettings["LuisAppId"].ToString(),
                        luisClientSettings["LuisSubscriptionKey"].ToString(),
                        luisClientSettings["BingSpellCheckApiKey"].ToString(),
                        _httpClient);


                    IList<Tuple<string, int>> matchingIntents =
                        await luisClient.GetMatchingIntents(userDescription);

                    conditionMatchData = matchingIntents.Where(i => i.Item1 != "None").ToList();
                    luisCalled = true;
                }
            }

            catch (HttpRequestException e)
            {
                log.LogError("Failed to get answer from LUIS.",e);
                return new StatusCodeResult(502);
            }

            //https://www.owasp.org/index.php/REST_Security_Cheat_Sheet
            req.HttpContext.Response.Headers.Add("X-Content-Type-Options", new[] {"nosniff"});
            req.HttpContext.Response.Headers.Add("X-Frame-Options", "deny");

            string responseBody;
            if (apiVersion == "v1")
            {
                responseBody = CreateLuisResponseData(conditionMatchData, userDescription, topValue, confidenceAsInt)
                    .ToString();
            }
            else
            {
                var matchesArray = CreateLuisResponseData(conditionMatchData, userDescription, topValue, confidenceAsInt);
                if (luisCalled)
                {
                    responseBody = BuildResponseBodyWithFlags(userDescription, conditionMatchData, matchesArray,
                        _multipleConditionIdentifier).ToString();
                }
                else
                {
                    responseBody = BuildResponseBodyWithEmptyFlags(matchesArray).ToString();
                }
            }
            
            var responseMessageResult = new ContentResult
            {
                Content =  responseBody,
                ContentType = "application/json; charset=utf-8"
            };

            return responseMessageResult;
        }


        private struct QueryParamsParseResult
        {
            public bool IsSuccess;
            public string ErrMsg;
            public int NumberOfConditionsToReturn;
            public int? ConfidenceThreshold;
            public string UserDescription;
            
            public static QueryParamsParseResult Fail(string errMsg)
            {
                return new QueryParamsParseResult()
                {
                    ErrMsg = errMsg,
                    IsSuccess = false
                };
            }
           
        }

        private static QueryParamsParseResult ParseQueryParams(HttpRequest req)
        {
            var userDescriptionQueryParamName = "conditionAsDescribedByUser";

            req.Query.TryGetValue(userDescriptionQueryParamName, out var userDescription);
            if (!userDescription.Any() || userDescription.Count > 1)
            {
                return QueryParamsParseResult.Fail(
                    $"Provide the single mandatory query parameter '{userDescriptionQueryParamName}'.");
            }

            var confidenceLevelQueryParamterOpt = "minimumConfidenceAsPercentage";
            req.Query.TryGetValue(confidenceLevelQueryParamterOpt, out var confidenceLevelQueryParamterOptValue);
            if (confidenceLevelQueryParamterOptValue.Count > 1)
            {
                return QueryParamsParseResult.Fail($"Only one value allowed for confidence threshold.");
            }

            var topOptName = "top";
            req.Query.TryGetValue(topOptName, out var topOptValue);
            if (confidenceLevelQueryParamterOptValue.Count > 1)
            {
                return QueryParamsParseResult.Fail($"Only one value allowed for number of recommendations to return.");
            }

            int topValue;
            if (topOptValue.Any())
            {
                try
                {
                    topValue = Convert.ToInt16(topOptValue.Single());
                    if (topValue == 0)
                    {
                        return QueryParamsParseResult.Fail("'Top' query param must be greater than 0.");
                    }
                }
                catch (FormatException)
                {
                    return QueryParamsParseResult.Fail("'Top' query param must be an integer larger than 0.");
                }
                catch (OverflowException)
                {
                    return QueryParamsParseResult.Fail("'Top' query parameter value is to too long.");
                }
            }
            else
            {
                topValue = 5;
            }

            int? confidenceAsInt = null;
            if (confidenceLevelQueryParamterOptValue.FirstOrDefault() != null)
            {
                var confidenceThresholdString = confidenceLevelQueryParamterOptValue.Single();
                try
                {
                    confidenceAsInt = Convert.ToInt16(confidenceThresholdString);
                }
                catch (FormatException)
                {
                    return QueryParamsParseResult.Fail(
                        "Confidence threshold must be an integer from 0 to 100 inclusive.");
                }
                catch (OverflowException)
                {
                    return QueryParamsParseResult.Fail(
                        "Confidence threshold must be an integer from 0 to 100 inclusive.");
                }
            }
            

            return new QueryParamsParseResult()
            {
                IsSuccess = true,
                UserDescription = userDescription,
                ConfidenceThreshold = confidenceAsInt,
                NumberOfConditionsToReturn = topValue
            };

        }


        private static JArray CreateLuisResponseData(IList<Tuple<string,int>> matchingIntents, StringValues userDescription, int topValue,
            int? confidenceAsInt)
        {
           
            var filteredIntents = FilterLuisIntents(topValue, confidenceAsInt, matchingIntents);
            var responseBody = BuildConditionMatchesArray(filteredIntents);
            // cache response body


            // analytics
            if (matchingIntents.FirstOrDefault() != null)
            {
                var topMatchesFromLuis =
                    matchingIntents.Take(5).ToDictionary(i => i.Item1, i => Convert.ToString(i.Item2));

                var topMatch = topMatchesFromLuis.OrderByDescending(kvp => Convert.ToInt16(kvp.Value))
                    .First();

                telemetry.TrackEvent("luis-match", topMatchesFromLuis);

                var luisMatchSummary = new Dictionary<string, string>();
                luisMatchSummary.Add("topMatchCondition", topMatch.Key);
                luisMatchSummary.Add("topMatchConfidence", topMatch.Value);
                luisMatchSummary.Add("userQuery", userDescription);
                telemetry.TrackEvent("luis-match-summary", luisMatchSummary);
            }

            return responseBody;
        }


        private static IList<Tuple<string, int>> FilterLuisIntents(int top, int? confidence,
            IList<Tuple<string, int>> data)
        {
            var topOnly = data.Take(top);
            var matchingCondidenceLevel = (
                confidence.HasValue ? topOnly.Where(i => i.Item2 >= confidence.Value) : topOnly).ToList();

            return matchingCondidenceLevel;
        }


        private static JObject BuildResponseBodyWithEmptyFlags(JArray conditionsArray)
        {
            var root = new JObject();
            root.Add(new JProperty("flags", new JArray()));

            root.Add(new JProperty("conditions", conditionsArray));
            return root;
        }

        private static JObject BuildResponseBodyWithFlags(string userQuery, IList<Tuple<string, int>> luisData, JArray matchingIntentsArray, IMultipleConditionIdentifier multipleConditionIdentifier)
        {
            var multipleConditionsResult =
                multipleConditionIdentifier.IsPossiblyMultipleCondition(userQuery, luisData.ToList());
            
            var root = new JObject();
            var flagsArray = new JArray();

            if (multipleConditionsResult.IsMultipleCondition)
            {
                flagsArray.Add(new JObject(
                            new JProperty("name","multipleConditions"),
                            new JProperty("description", "The user's description of their condition may cover more than one SoP condition."),
                            new JProperty("value", multipleConditionsResult.IsMultipleCondition),
                            new JProperty("reason", multipleConditionsResult.Reason)
                    ));
            }

            root.Add(new JProperty("flags",flagsArray));
            
            root.Add(new JProperty("conditions",matchingIntentsArray));
            return root;
        }
        
        private static JArray BuildConditionMatchesArray(IList<Tuple<string, int>> luisData)
        {
            if (!luisData.Any())
            {
                return new JArray();
            }
            else
            {
                var root = new JArray();
                foreach (var tuple in luisData)
                {
                    root.Add(new JObject(
                        new JProperty("currentInForceSopConditionName", tuple.Item1),
                        new JProperty("confidencePercentage", tuple.Item2)));
                }

                return root;
            }
        }
    }
}