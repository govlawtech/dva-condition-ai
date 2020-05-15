using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConditionMatcherAzureFunction.Model;
using Newtonsoft.Json.Linq;

namespace ConditionMatcherAzureFunction
{
    static class Utils
    {
        public static IEnumerable<SoPCondition> DeserialiseConditionsFromFileSnapshot(string json)
        {
            JObject snapshotJson = JObject.Parse(json);
            JArray conditionsArray = (JArray)snapshotJson["conditions"];

            var conditions = from jtoken in conditionsArray
                let name = jtoken["conditionName"].ToString()
                let rhRegisterId = jtoken["rhRegisterId"].ToString()
                let bopRegisterId = jtoken["bopRegisterId"].ToString()
                let icdCodes = jtoken["icdCodes"].Select(c => c["code"].ToString())
                select new SoPCondition()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    RH_RegisterId = rhRegisterId,
                    BoP_RegisterId = bopRegisterId,
                    ICD_Codes = icdCodes
                };

            return conditions;
        }
    }
}
