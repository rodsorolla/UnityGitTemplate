namespace Sorolla.PersistentData
{
    /// <summary>
    /// Base interface for all saveable data classes.
    /// Implement this interface on any class you want to persist.
    /// </summary>
    public interface ISaveData
    {
        /// <summary>
        /// The version of this data structure.
        /// Increment when making breaking changes to enable migrations.
        /// </summary>
        int Version { get; }
    }
}
