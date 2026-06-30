namespace Sorolla.Events
{
    /// <summary>
    /// Optional metadata passed alongside CommitRun. Game-side hooks fill it in.
    /// </summary>
    public sealed class EventCommitContext
    {
        /// <summary>1-based progressive level index when the run started.</summary>
        public int ProgressiveLevelIndex;

        /// <summary>True when the level is hard-tier (multiplier already applied to collector).</summary>
        public bool WasHardLevel;
    }
}
