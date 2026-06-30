using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Sorolla.PersistentData
{
    /// <summary>
    /// Main API for the persistent data system.
    /// Provides static methods for saving, loading, and managing game data.
    /// </summary>
    public static class SaveSystem
    {
        private static IStorageProvider _storage;
        private static BackupManager _backups;
        private static JsonSerializerSettings _jsonSettings;
        private static bool _initialized;

        /// <summary>
        /// Backup manager for save file backups.
        /// </summary>
        public static BackupManager Backups
        {
            get
            {
                EnsureInitialized();
                return _backups;
            }
        }

        /// <summary>
        /// The current storage provider.
        /// </summary>
        public static IStorageProvider Storage
        {
            get
            {
                EnsureInitialized();
                return _storage;
            }
        }

        /// <summary>
        /// Initializes the save system with default settings.
        /// Called automatically on first use.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            var localStorage = new LocalFileStorage();
            _storage = localStorage;
            _backups = new BackupManager(localStorage.BasePath);

            _jsonSettings = new JsonSerializerSettings
            {
                // None (not Auto): save files live in persistentDataPath and are user-writable
                // (cloud save, SaveDataEditorWindow, rooted devices). TypeNameHandling.Auto honors
                // "$type" directives and would instantiate arbitrary types from a tampered file
                // (Newtonsoft deserialization-gadget vector). All ISaveData types are concrete, so
                // None is safe. Add a SerializationBinder whitelist if polymorphism is ever needed.
                TypeNameHandling = TypeNameHandling.None,
                Formatting = Application.isEditor ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include,
                DateFormatString = "o" // ISO 8601
            };

            _initialized = true;
        }

        /// <summary>
        /// Initializes the save system with a custom storage provider.
        /// </summary>
        public static void Initialize(IStorageProvider storageProvider, string backupBasePath = null)
        {
            _storage = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            _backups = new BackupManager(backupBasePath ?? Application.persistentDataPath);

            _jsonSettings = new JsonSerializerSettings
            {
                // None (not Auto): save files live in persistentDataPath and are user-writable
                // (cloud save, SaveDataEditorWindow, rooted devices). TypeNameHandling.Auto honors
                // "$type" directives and would instantiate arbitrary types from a tampered file
                // (Newtonsoft deserialization-gadget vector). All ISaveData types are concrete, so
                // None is safe. Add a SerializationBinder whitelist if polymorphism is ever needed.
                TypeNameHandling = TypeNameHandling.None,
                Formatting = Application.isEditor ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include,
                DateFormatString = "o"
            };

            _initialized = true;
        }

        /// <summary>
        /// Saves data to a file.
        /// </summary>
        /// <typeparam name="T">The data type (must implement ISaveData)</typeparam>
        /// <param name="data">The data to save</param>
        /// <param name="fileName">Name of the save file (without extension)</param>
        /// <param name="slot">Save slot number (0 = default slot)</param>
        /// <param name="createBackup">Whether to backup existing file before overwriting</param>
        /// <returns>Result of the save operation</returns>
        public static SaveResult Save<T>(T data, string fileName, int slot = 0, bool createBackup = true) where T : ISaveData
        {
            EnsureInitialized();

            if (createBackup)
            {
                var filePath = _storage.GetFilePath(fileName, slot);
                _backups.CreateBackup(filePath);
            }

            try
            {
                var json = JsonConvert.SerializeObject(data, _jsonSettings);
                return _storage.Save(json, fileName, slot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Serialization failed for {fileName}: {ex.Message}");
                return SaveResult.Fail(_storage.GetFilePath(fileName, slot), ex);
            }
        }

        /// <summary>
        /// Saves data to a file asynchronously.
        /// </summary>
        public static async UniTask<SaveResult> SaveAsync<T>(T data, string fileName, int slot = 0, bool createBackup = true) where T : ISaveData
        {
            EnsureInitialized();

            if (createBackup)
            {
                var filePath = _storage.GetFilePath(fileName, slot);
                _backups.CreateBackup(filePath);
            }

            try
            {
                var json = await UniTask.RunOnThreadPool(() => JsonConvert.SerializeObject(data, _jsonSettings));
                return await _storage.SaveAsync(json, fileName, slot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Serialization failed for {fileName}: {ex.Message}");
                return SaveResult.Fail(_storage.GetFilePath(fileName, slot), ex);
            }
        }

        /// <summary>
        /// Loads data from a file.
        /// Returns a new instance if the file doesn't exist or is corrupted.
        /// </summary>
        /// <typeparam name="T">The data type (must implement ISaveData and have a parameterless constructor)</typeparam>
        /// <param name="fileName">Name of the save file (without extension)</param>
        /// <param name="slot">Save slot number (0 = default slot)</param>
        /// <returns>The loaded data or a new default instance</returns>
        public static T Load<T>(string fileName, int slot = 0) where T : ISaveData, new()
        {
            return Load<T>(fileName, slot, defaultValue: new T());
        }

        /// <summary>
        /// Loads data from a file with a custom default value.
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="fileName">Name of the save file</param>
        /// <param name="slot">Save slot number</param>
        /// <param name="defaultValue">Value to return if load fails</param>
        /// <returns>The loaded data or the default value</returns>
        public static T Load<T>(string fileName, int slot, T defaultValue) where T : ISaveData
        {
            EnsureInitialized();

            var json = _storage.Load(fileName, slot);

            if (string.IsNullOrEmpty(json))
            {
                return defaultValue;
            }

            if (!SaveValidator.IsValidJson(json))
            {
                Debug.LogWarning($"[SaveSystem] Invalid JSON in {fileName}, using default");
                return defaultValue;
            }

            if (!SaveValidator.TryDeserialize<T>(json, out var result, _jsonSettings))
            {
                Debug.LogWarning($"[SaveSystem] Deserialization failed for {fileName}, using default");
                return defaultValue;
            }

            return result;
        }

        /// <summary>
        /// Loads data from a file asynchronously.
        /// </summary>
        public static UniTask<T> LoadAsync<T>(string fileName, int slot = 0) where T : ISaveData, new()
        {
            return LoadAsync<T>(fileName, slot, defaultValue: new T());
        }

        /// <summary>
        /// Loads data from a file asynchronously with a custom default value.
        /// </summary>
        public static async UniTask<T> LoadAsync<T>(string fileName, int slot, T defaultValue) where T : ISaveData
        {
            EnsureInitialized();

            var json = await _storage.LoadAsync(fileName, slot);

            if (string.IsNullOrEmpty(json))
            {
                return defaultValue;
            }

            if (!SaveValidator.IsValidJson(json))
            {
                Debug.LogWarning($"[SaveSystem] Invalid JSON in {fileName}, using default");
                return defaultValue;
            }

            var result = await UniTask.RunOnThreadPool(() =>
            {
                if (SaveValidator.TryDeserialize<T>(json, out var data, _jsonSettings))
                    return data;
                return default;
            });

            if (result == null)
            {
                Debug.LogWarning($"[SaveSystem] Deserialization failed for {fileName}, using default");
                return defaultValue;
            }

            return result;
        }

        /// <summary>
        /// Checks if a save file exists.
        /// </summary>
        public static bool Exists(string fileName, int slot = 0)
        {
            EnsureInitialized();
            return _storage.Exists(fileName, slot);
        }

        /// <summary>
        /// Deletes a save file and optionally its backups.
        /// </summary>
        public static bool Delete(string fileName, int slot = 0, bool deleteBackups = false)
        {
            EnsureInitialized();

            var result = _storage.Delete(fileName, slot);

            if (deleteBackups)
                _backups.DeleteAllBackups(fileName);

            return result;
        }

        /// <summary>
        /// Deletes all save files and backups in the specified slot.
        /// If slot is -1, deletes all slots.
        /// </summary>
        public static void DeleteAllData(int slot = -1)
        {
            EnsureInitialized();

            if (slot == -1)
            {
                var basePath = (_storage as LocalFileStorage)?.BasePath;
                if (basePath != null
                    && basePath.StartsWith(Application.persistentDataPath)
                    && Directory.Exists(basePath))
                {
                    Directory.Delete(basePath, true);
                    Debug.Log("[SaveSystem] All save data deleted.");
                }
            }
            else
            {
                var files = GetAllSaveFiles(slot);
                foreach (var file in files)
                    Delete(file, slot, deleteBackups: true);
                Debug.Log($"[SaveSystem] All data in slot {slot} deleted.");
            }
        }

        /// <summary>
        /// Gets the full path to a save file.
        /// </summary>
        public static string GetFilePath(string fileName, int slot = 0)
        {
            EnsureInitialized();
            return _storage.GetFilePath(fileName, slot);
        }

        /// <summary>
        /// Gets all save file names in a slot.
        /// </summary>
        public static string[] GetAllSaveFiles(int slot = 0)
        {
            EnsureInitialized();
            return _storage.GetAllSaveFiles(slot);
        }

        /// <summary>
        /// Resets the save system (useful for testing).
        /// </summary>
        public static void Reset()
        {
            _storage = null;
            _backups = null;
            _jsonSettings = null;
            _initialized = false;
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
                Initialize();
        }
    }
}
