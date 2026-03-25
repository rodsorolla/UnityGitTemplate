using Cysharp.Threading.Tasks;

namespace Sorolla.PersistentData
{
    /// <summary>
    /// Abstract storage interface for swappable backends.
    /// Implement this to add cloud storage, encrypted storage, etc.
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Saves JSON string to storage.
        /// </summary>
        /// <param name="json">The JSON string to save</param>
        /// <param name="fileName">Name of the save file (without extension)</param>
        /// <param name="slot">Save slot number (0 = default slot)</param>
        /// <returns>Result of the save operation</returns>
        SaveResult Save(string json, string fileName, int slot = 0);

        /// <summary>
        /// Saves JSON string to storage asynchronously.
        /// </summary>
        UniTask<SaveResult> SaveAsync(string json, string fileName, int slot = 0);

        /// <summary>
        /// Loads JSON string from storage.
        /// </summary>
        /// <param name="fileName">Name of the save file (without extension)</param>
        /// <param name="slot">Save slot number (0 = default slot)</param>
        /// <returns>The loaded JSON string, or null if not found</returns>
        string Load(string fileName, int slot = 0);

        /// <summary>
        /// Loads JSON string from storage asynchronously.
        /// </summary>
        UniTask<string> LoadAsync(string fileName, int slot = 0);

        /// <summary>
        /// Checks if a save file exists.
        /// </summary>
        bool Exists(string fileName, int slot = 0);

        /// <summary>
        /// Deletes a save file.
        /// </summary>
        /// <returns>True if deleted successfully or file didn't exist</returns>
        bool Delete(string fileName, int slot = 0);

        /// <summary>
        /// Gets the full path to a save file.
        /// </summary>
        string GetFilePath(string fileName, int slot = 0);

        /// <summary>
        /// Gets all save file names in a slot.
        /// </summary>
        string[] GetAllSaveFiles(int slot = 0);
    }
}
