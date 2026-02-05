using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.PersistentData
{
    /// <summary>
    /// Manages version migrations for save data.
    /// Supports chained migrations (v1→v2→v3).
    /// </summary>
    public class MigrationPipeline
    {
        private readonly Dictionary<string, List<IMigrator>> _migrators = new();

        /// <summary>
        /// Registers a migration function for a specific type and version transition.
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="fromVersion">Version to migrate from</param>
        /// <param name="toVersion">Version to migrate to</param>
        /// <param name="migrationFunc">Function that transforms JSON from old to new version</param>
        public void Register<T>(int fromVersion, int toVersion, Func<string, string> migrationFunc) where T : ISaveData
        {
            var typeName = typeof(T).FullName;
            var migrator = new FuncMigrator(typeName, fromVersion, toVersion, migrationFunc);
            Register(migrator);
        }

        /// <summary>
        /// Registers a migrator instance.
        /// </summary>
        public void Register(IMigrator migrator)
        {
            if (!_migrators.TryGetValue(migrator.TypeName, out var list))
            {
                list = new List<IMigrator>();
                _migrators[migrator.TypeName] = list;
            }

            // Insert sorted by FromVersion
            int index = list.FindIndex(m => m.FromVersion > migrator.FromVersion);
            if (index < 0)
                list.Add(migrator);
            else
                list.Insert(index, migrator);

            Debug.Log($"[SaveSystem] Registered migration for {migrator.TypeName}: v{migrator.FromVersion} → v{migrator.ToVersion}");
        }

        /// <summary>
        /// Migrates JSON data from its current version to the target version.
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="json">The JSON string to migrate</param>
        /// <param name="currentVersion">Current version of the data</param>
        /// <param name="targetVersion">Target version to migrate to</param>
        /// <param name="migratedJson">The migrated JSON string</param>
        /// <returns>True if migration succeeded (or no migration needed)</returns>
        public bool TryMigrate<T>(string json, int currentVersion, int targetVersion, out string migratedJson) where T : ISaveData
        {
            migratedJson = json;

            if (currentVersion >= targetVersion)
                return true;

            var typeName = typeof(T).FullName;

            if (!_migrators.TryGetValue(typeName, out var migrators))
            {
                Debug.LogWarning($"[SaveSystem] No migrators registered for {typeName}. Data version {currentVersion} may be incompatible with {targetVersion}.");
                return false;
            }

            var currentJson = json;
            var version = currentVersion;

            while (version < targetVersion)
            {
                var migrator = migrators.Find(m => m.FromVersion == version);

                if (migrator == null)
                {
                    Debug.LogWarning($"[SaveSystem] No migrator found for {typeName} from version {version}. Migration chain broken.");
                    return false;
                }

                try
                {
                    currentJson = migrator.Migrate(currentJson);
                    var newVersion = migrator.ToVersion;
                    Debug.Log($"[SaveSystem] Migrated {typeName}: v{version} → v{newVersion}");
                    version = newVersion;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveSystem] Migration failed for {typeName} v{version}: {ex.Message}");
                    return false;
                }
            }

            // Update the version in the JSON
            migratedJson = SaveValidator.SetProperty(currentJson, "Version", targetVersion);
            return true;
        }

        /// <summary>
        /// Checks if a migration path exists from one version to another.
        /// </summary>
        public bool HasMigrationPath<T>(int fromVersion, int toVersion) where T : ISaveData
        {
            if (fromVersion >= toVersion)
                return true;

            var typeName = typeof(T).FullName;

            if (!_migrators.TryGetValue(typeName, out var migrators))
                return false;

            var version = fromVersion;
            while (version < toVersion)
            {
                var migrator = migrators.Find(m => m.FromVersion == version);
                if (migrator == null)
                    return false;
                version = migrator.ToVersion;
            }

            return true;
        }

        /// <summary>
        /// Clears all registered migrators.
        /// </summary>
        public void Clear()
        {
            _migrators.Clear();
        }

        /// <summary>
        /// Internal migrator using a function delegate.
        /// </summary>
        private class FuncMigrator : IMigrator
        {
            private readonly Func<string, string> _migrateFunc;

            public string TypeName { get; }
            public int FromVersion { get; }
            public int ToVersion { get; }

            public FuncMigrator(string typeName, int fromVersion, int toVersion, Func<string, string> migrateFunc)
            {
                TypeName = typeName;
                FromVersion = fromVersion;
                ToVersion = toVersion;
                _migrateFunc = migrateFunc;
            }

            public string Migrate(string json) => _migrateFunc(json);
        }
    }
}
