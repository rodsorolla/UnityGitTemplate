namespace Sorolla.PersistentData
{
    /// <summary>
    /// Base interface for all saveable data classes.
    /// Implement this interface on any class you want to persist.
    /// </summary>
    public interface ISaveData
    {
        /// <summary>
        /// Reserved for future migration support. The current SaveSystem does not
        /// branch on this value — it is serialized into every save file as a
        /// forward-compatibility hook. Increment when making breaking changes so a
        /// future migration pipeline can detect the schema.
        /// </summary>
        int Version { get; }
    }
}
