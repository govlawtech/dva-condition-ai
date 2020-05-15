using System;
using System.Collections.Generic;
using ConditionMatcherAzureFunction.Model;

namespace ConditionMatcherAzureFunction.Interfaces
{
    interface IMultipleConditionIdentifier
    {
        MultipleConditionIdentifierResult IsPossiblyMultipleCondition(string userQuery, List<Tuple<string,int>> luisResults);
    }
}
