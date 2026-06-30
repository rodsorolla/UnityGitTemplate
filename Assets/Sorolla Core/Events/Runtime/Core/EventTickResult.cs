namespace Sorolla.Events
{
    /// <summary>
    /// Diagnostic result of an EventManager scheduler tick. Surfaced for
    /// debug overlays and tests.
    /// </summary>
    public readonly struct EventTickResult
    {
        public readonly string PreviousActiveId;
        public readonly string CurrentActiveId;
        public readonly bool CutoverHappened;

        public EventTickResult(string previousId, string currentId, bool cutover)
        {
            PreviousActiveId = previousId;
            CurrentActiveId = currentId;
            CutoverHappened = cutover;
        }
    }
}
