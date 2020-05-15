using System.Collections.Generic;

namespace ConditionMatcherAzureFunction.Model
{
    public class SoPCondition
    {

        public string Id
        {
            get; set;
        }

        public IEnumerable<string> PreviouslyAccepted
        {
            get; set;
        }

        public string Name
        {
            get; set;
        }


        public string RH_RegisterId
        {
            get; set;
        }

        public string BoP_RegisterId
        {
            get; set;
        }

        public IEnumerable<string> ICD_Codes
        {
            get; set;
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
