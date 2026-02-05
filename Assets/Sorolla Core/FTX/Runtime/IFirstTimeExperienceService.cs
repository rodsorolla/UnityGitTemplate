namespace Sorolla.FTX
{
    /// <summary>
    /// Service for managing first-time hints and experiences.
    /// Tracks which features/screens have been seen by the player.
    /// </summary>
    public interface IFirstTimeExperienceService
    {
        /// <summary>
        /// Checks if a key has been seen.
        /// </summary>
        bool HasSeen(string key);

        /// <summary>
        /// Marks a key as seen.
        /// </summary>
        void MarkAsSeen(string key);

        /// <summary>
        /// Checks if this is the first time for a key.
        /// If first time, automatically marks it as seen.
        /// </summary>
        /// <returns>True if first time (was not seen before), false otherwise</returns>
        bool CheckFirstTime(string key);
    }
}
