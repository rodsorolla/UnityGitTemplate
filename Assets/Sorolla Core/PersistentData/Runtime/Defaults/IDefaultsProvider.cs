namespace Sorolla.PersistentData
{
    /// <summary>
    /// Interface for ScriptableObjects that provide default values for save data.
    /// Implement this on your config SOs to enable automatic default initialization.
    /// </summary>
    /// <typeparam name="T">The save data type</typeparam>
    public interface IDefaultsProvider<T> where T : ISaveData
    {
        /// <summary>
        /// Creates a new instance of T with default values from this config.
        /// </summary>
        /// <returns>A new instance with default values</returns>
        T CreateDefault();
    }
}
