namespace Sorolla.Events
{
    /// <summary>Metadata passed to IRewardGranter for analytics/UX.</summary>
    public sealed class RewardGrantContext
    {
        public string EventId;
        public int StepIndex;       // -1 for Grand Prize
        public bool IsGrandPrize;
    }
}
