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

                // Unique temp per write so concurrent saves to the same file don't collide
                // on a shared temp path (Sharing violation / "file not found" during rename).
                var tmpPath = filePath + ".tmp-" + Guid.NewGuid().ToString("N");
                File.WriteAllText(tmpPath, json);
                ReplaceFile(tmpPath, filePath);
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

                // Unique temp per write so concurrent saves to the same file don't collide
                // on a shared temp path (Sharing violation / "file not found" during rename).
                var tmpPath = filePath + ".tmp-" + Guid.NewGuid().ToString("N");
                await UniTask.RunOnThreadPool(() =>
                {
                    File.WriteAllText(tmpPath, json);
                    ReplaceFile(tmpPath, filePath);
                });
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

        /// <summary>
        /// Replaces the target file with the source. Uses File.Replace when the
        /// target exists so a crash between operations cannot leave the user with
        /// neither the old save nor the new one (delete-then-move would). Each writer
        /// owns a unique source temp file, so concurrent writes don't fight over it;
        /// if a concurrent write created the target between the check and the move,
        /// fall back to File.Replace.
        /// </summary>
        private static readonly object _renameLock = new object();

        private static void ReplaceFile(string sourcePath, string targetPath)
        {
            // Serialize the (fast) atomic rename so concurrent writers — each with its own
            // unique temp — can't race the final replace. Contention is negligible.
            lock (_renameLock)
            {
                if (File.Exists(targetPath))
                {
                    File.Replace(sourcePath, targetPath, destinationBackupFileName: null);
                    return;
                }

                try
                {
                    File.Move(sourcePath, targetPath);
                }
                catch (IOException)
                {
                    // Target appeared concurrently (another process) between check and move.
                    File.Replace(sourcePath, targetPath, destinationBackupFileName: null);
                }
            }
        }
    }
}
