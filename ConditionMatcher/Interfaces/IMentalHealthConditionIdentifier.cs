using System.Threading.Tasks;
using ConditionMatcherAzureFunction.Model;

namespace ConditionMatcherAzureFunction.Interfaces
{
    interface IMentalHealthConditionIdentifier
    {
        Task<MentalHealthConditionResult> IsPossiblyMentalHealthCondition(string userQuery);
    }
}