using System.Threading.Tasks;

namespace Sorolla.PersistentData
{
    /// <summary>
    /// Service interface for centralized game data management.
    /// Register with ServiceLocator for easy access throughout the game.
    /// </summary>
    public interface IGameDataService
    {
        /// <summary>
        /// Whether all data has been loaded.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Load all game data. Call once at game startup.
        /// </summary>
        Task LoadAllAsync();

        /// <summary>
        /// Save all game data.
        /// </summary>
        Task SaveAllAsync();

        /// <summary>
        /// Save all game data synchronously (for OnApplicationQuit).
        /// </summary>
        void SaveAll();

        /// <summary>
        /// Delete all save data and reset to defaults.
        /// </summary>
        void DeleteAll();
    }
}
