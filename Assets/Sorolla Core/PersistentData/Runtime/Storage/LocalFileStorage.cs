using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sorolla.PersistentData
{
    /// <summary>
    /// File-based storage provider using Application.persistentDataPath.
    /// </summary>
    public class LocalFileStorage : IStorageProvider
    {
        private const string SavesFolder = "saves";
        private const string DefaultSlotName = "default";
        private const string FileExtension = ".json";

        private readonly string _basePath;

        public LocalFileStorage()
        {
            _basePath = Path.Combine(Application.persistentDataPath, SavesFolder);
        }

        /// <summary>
        /// Creates a LocalFileStorage with a custom base path (useful for testing).
        /// </summary>
        public LocalFileStorage(string basePath)
        {
            _basePath = basePath;
        }

        public SaveResult Save(string json, string fileName, int slot = 0)
        {
            var filePath = GetFilePath(fileName, slot);

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(filePath, json);
                return SaveResult.Ok(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save {fileName}: {ex.Message}");
                return SaveResult.Fail(filePath, ex);
            }
        }

        public async UniTask<SaveResult> SaveAsync(string json, string fileName, int slot = 0)
        {
            var filePath = GetFilePath(fileName, slot);

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await UniTask.RunOnThreadPool(() => File.WriteAllText(filePath, json));
                return SaveResult.Ok(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save {fileName}: {ex.Message}");
                return SaveResult.Fail(filePath, ex);
            }
        }

        public string Load(string fileName, int slot = 0)
        {
            var filePath = GetFilePath(fileName, slot);

            try
            {
                if (!File.Exists(filePath))
                    return null;

                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load {fileName}: {ex.Message}");
                return null;
            }
        }

        public async UniTask<string> LoadAsync(string fileName, int slot = 0)
        {
            var filePath = GetFilePath(fileName, slot);

            try
            {
                if (!File.Exists(filePath))
                    return null;

                return await UniTask.RunOnThreadPool(() => File.ReadAllText(filePath));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load {fileName}: {ex.Message}");
                return null;
            }
        }

        public bool Exists(string fileName, int slot = 0)
        {
            return File.Exists(GetFilePath(fileName, slot));
        }

        public bool Delete(string fileName, int slot = 0)
        {
            var filePath = GetFilePath(fileName, slot);

            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to delete {fileName}: {ex.Message}");
                return false;
            }
        }

        public string GetFilePath(string fileName, int slot = 0)
        {
            var slotFolder = slot == 0 ? DefaultSlotName : $"slot{slot}";
            return Path.Combine(_basePath, slotFolder, fileName + FileExtension);
        }

        public string[] GetAllSaveFiles(int slot = 0)
        {
            var slotFolder = slot == 0 ? DefaultSlotName : $"slot{slot}";
            var slotPath = Path.Combine(_basePath, slotFolder);

            if (!Directory.Exists(slotPath))
                return Array.Empty<string>();

            var files = Directory.GetFiles(slotPath, "*" + FileExtension);
            var names = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
                names[i] = Path.GetFileNameWithoutExtension(files[i]);

            return names;
        }

        /// <summary>
        /// Gets the base saves directory path.
        /// </summary>
        public string BasePath => _basePath;
    }
}
