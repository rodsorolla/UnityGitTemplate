namespace Sorolla
{
    /// <summary>
    /// Abstraction for persistence operations.
    /// Allows Sorolla Core components to save/load data without knowing storage implementation.
    /// </summary>
    public interface IPersistenceService
    {
        /// <summary>
        /// Save an integer value with the given key.
        /// </summary>
        void SaveInt(string key, int value);
        
        /// <summary>
        /// Load an integer value with the given key, returns default if not found.
        /// </summary>
        int LoadInt(string key, int defaultValue = 0);
        
        /// <summary>
        /// Save a string value with the given key.
        /// </summary>
        void SaveString(string key, string value);
        
        /// <summary>
        /// Load a string value with the given key, returns default if not found.
        /// </summary>
        string LoadString(string key, string defaultValue = "");
        
        /// <summary>
        /// Check if a key exists.
        /// </summary>
        bool HasKey(string key);
        
        /// <summary>
        /// Delete a key.
        /// </summary>
        void DeleteKey(string key);
        
        /// <summary>
        /// Flush changes to persistent storage.
        /// </summary>
        void Save();
    }
}

