using System.Threading.Tasks;
using UnityEngine;

namespace Sorolla.PersistentData
{
    /// <summary>
    /// Base class for game data management with auto-save on app pause/quit.
    /// Extend this class and add your specific data properties.
    ///
    /// Usage:
    /// 1. Create a subclass with your data properties
    /// 2. Override LoadAllAsync() and SaveAllAsync() to load/save your data
    /// 3. Register with ServiceLocator in your game initialization
    /// 4. Attach to a GameObject for auto-save lifecycle hooks
    /// </summary>
    public abstract class GameDataServiceBase : MonoBehaviour, IGameDataService
    {
        [Header("Auto-Save Settings")]
        [SerializeField] private bool _saveOnPause = true;
        [SerializeField] private bool _saveOnQuit = true;

        private bool _isLoaded;

        public bool IsLoaded => _isLoaded;

        protected virtual void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationPause(bool paused)
        {
            if (paused && _saveOnPause && _isLoaded)
            {
                SaveAll();
                Debug.Log("[GameDataService] Auto-saved on pause");
            }
        }

        protected virtual void OnApplicationQuit()
        {
            if (_saveOnQuit && _isLoaded)
            {
                SaveAll();
                Debug.Log("[GameDataService] Auto-saved on quit");
            }
        }

        /// <summary>
        /// Load all game data. Override to load your specific data.
        /// </summary>
        public virtual async Task LoadAllAsync()
        {
            _isLoaded = true;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Save all game data asynchronously. Override to save your specific data.
        /// </summary>
        public virtual async Task SaveAllAsync()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Save all game data synchronously. Override if you need custom sync save.
        /// </summary>
        public virtual void SaveAll()
        {
            // Default implementation runs async save synchronously
            SaveAllAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Delete all saves and reset to defaults. Override to handle your data.
        /// </summary>
        public virtual void DeleteAll()
        {
            _isLoaded = false;
        }
    }
}
