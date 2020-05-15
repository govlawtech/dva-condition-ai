namespace ConditionMatcherAzureFunction.Model
{
    struct MentalHealthConditionResult
    {
        public bool IsMentalHealthCondition { get; }
        public string Reason { get; }

        public MentalHealthConditionResult(bool isMentalHealthCondition, string reason)
        {
            IsMentalHealthCondition = isMentalHealthCondition;
            Reason = reason;
        }
    }
}