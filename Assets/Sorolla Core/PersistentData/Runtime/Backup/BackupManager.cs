using System;
using System.IO;
using UnityEngine;
using ZLinq;

namespace Sorolla.PersistentData
{
    /// <summary>
    /// Manages timestamped backups of save files.
    /// </summary>
    public class BackupManager
    {
        private const string BackupFolder = "backups";
        private const string TimestampFormat = "yyyyMMdd_HHmmss";

        private readonly string _basePath;
        private int _maxBackups = 3;

        public BackupManager(string basePath)
        {
            _basePath = basePath;
        }

        /// <summary>
        /// Maximum number of backups to keep per file. Default is 3.
        /// </summary>
        public int MaxBackups
        {
            get => _maxBackups;
            set => _maxBackups = Mathf.Max(1, value);
        }

        /// <summary>
        /// Creates a backup of an existing save file before overwriting.
        /// </summary>
        /// <param name="originalFilePath">Path to the file to backup</param>
        /// <returns>True if backup was created or file didn't exist</returns>
        public bool CreateBackup(string originalFilePath)
        {
            if (!File.Exists(originalFilePath))
                return true;

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
                var extension = Path.GetExtension(originalFilePath);
                var timestamp = DateTime.Now.ToString(TimestampFormat);
                var backupFileName = $"{fileName}_{timestamp}{extension}";

                var backupDir = Path.Combine(_basePath, BackupFolder);
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                var backupPath = Path.Combine(backupDir, backupFileName);
                File.Copy(originalFilePath, backupPath, overwrite: true);

                CleanupOldBackups(fileName, extension);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to create backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all backup files for a given save file name.
        /// </summary>
        /// <param name="fileName">Original file name (without extension)</param>
        /// <returns>Array of backup file paths, sorted newest first</returns>
        public string[] GetBackups(string fileName)
        {
            var backupDir = Path.Combine(_basePath, BackupFolder);
            if (!Directory.Exists(backupDir))
                return Array.Empty<string>();

            var pattern = $"{fileName}_*";
            var files = Directory.GetFiles(backupDir, pattern)
                .AsValueEnumerable()
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToArray();

            return files;
        }

        /// <summary>
        /// Deletes all backups for a save file.
        /// </summary>
        public void DeleteAllBackups(string fileName)
        {
            var backups = GetBackups(fileName);
            foreach (var backup in backups)
            {
                try
                {
                    File.Delete(backup);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveSystem] Failed to delete backup {backup}: {ex.Message}");
                }
            }
        }

        private void CleanupOldBackups(string fileName, string extension)
        {
            var backups = GetBackups(fileName);

            if (backups.Length <= _maxBackups)
                return;

            // Delete oldest backups beyond the limit
            for (int i = _maxBackups; i < backups.Length; i++)
            {
                try
                {
                    File.Delete(backups[i]);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveSystem] Failed to delete old backup: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets the backup directory path.
        /// </summary>
        public string BackupDirectory => Path.Combine(_basePath, BackupFolder);
    }
}
