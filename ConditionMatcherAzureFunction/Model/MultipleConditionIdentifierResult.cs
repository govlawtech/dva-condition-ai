namespace ConditionMatcherAzureFunction.Model
{
    struct MultipleConditionIdentifierResult
    {
        public bool IsMultipleCondition { get; }
        public string Reason { get; }

        public MultipleConditionIdentifierResult(bool isMultipleCondition, string reason)
        {
            IsMultipleCondition = isMultipleCondition;
            Reason = reason;
        }

    }
}