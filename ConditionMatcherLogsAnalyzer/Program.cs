using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json.Linq;

namespace ConditionMatcherLogsAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceDataPath = args[0];
            List<List<string>> rows = new List<List<string>>();
            using (TextFieldParser parser = new TextFieldParser(sourceDataPath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.ReadLine();
                while (!parser.EndOfData)
                {
                    //Processing row
                    string[] fields = parser.ReadFields();
                    rows.Add(fields.ToList());
                   
                }
            }

            var topMatches = from r in rows
                let cd = JObject.Parse(r.ElementAt(3))
                select new
                {
                    Row = r,
                    CustomDimensions = cd
                };

            var inferredData = (from tm in topMatches
                where tm.CustomDimensions.ContainsKey("userQuery")
                let userQuery = tm.CustomDimensions["userQuery"].Value<string>()
                let topMatchCondition = tm.CustomDimensions["topMatchCondition"].Value<string>()
                let topMatchConfidence = tm.CustomDimensions["topMatchConfidence"].Value<string>()
                select new
                {
                    Query = userQuery,
                    Result = topMatchCondition,
                    Confidence = topMatchConfidence
                }).GroupBy(a => a.Query);


            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Query,Result,Confidence");
            foreach (var id in inferredData)
            {
                sb.AppendLine($"\"{id.Key}\",\"{id.First().Result}\", \"{id.First().Confidence}\"");
            }

            File.WriteAllText("aiConditionMatcherConfidenceResults.csv",sb.ToString());
                        

            Console.WriteLine("Have a nice day");

        }
    }
}
