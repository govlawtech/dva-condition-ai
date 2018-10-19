using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConditionMatcherAzureFunction.Interfaces;
using ConditionMatcherAzureFunction.Model;

namespace ConditionMatcherAzureFunction.Implementations
{
    class MultipleConditionIdentifier : IMultipleConditionIdentifier
    {
        private static readonly int CONFIDENCE_THRESHOLD_FOR_MULTIPLE_CONDITIONS = 95;
        private static readonly int CHARACTER_LIMIT = 75; 
        private static readonly Regex CONJUNCTION_REGEX = new Regex(@"(\s+and\s+)|,");

        public MultipleConditionIdentifierResult IsPossiblyMultipleCondition(string userQuery, List<Tuple<string, int>> luisResults)
        {
            // strategies:
            // multiple conditions with high confidence;
            // contains comma or and, and does not match known condition
            // length of description in top percentile
            // flag if any are true

            List<string> reasons = new List<string>();

            var conditionsWithHighConfidence = luisResults.Where(t => t.Item2 >= CONFIDENCE_THRESHOLD_FOR_MULTIPLE_CONDITIONS).ToList();
            if (conditionsWithHighConfidence.Count() > 1)
            {
                reasons.Add($"The AI identified multiple SoP conditions with high confidence: {String.Join(";",conditionsWithHighConfidence.Select(t => t.Item1))}");
            }

            if (userQuery.Length > CHARACTER_LIMIT)
            {
                reasons.Add("The description of the diagnosis was abnormally long.");
            }

            if (CONJUNCTION_REGEX.Matches(userQuery).Count > 0)
            {
                reasons.Add("The description of the diagnosis contained conjunctions.");
            }

            if (reasons.Any())
            {
                return new MultipleConditionIdentifierResult(true,String.Join(Environment.NewLine,reasons));
            }
            else
            {
                return new MultipleConditionIdentifierResult(false,String.Empty);
            }
        }

    }
}
