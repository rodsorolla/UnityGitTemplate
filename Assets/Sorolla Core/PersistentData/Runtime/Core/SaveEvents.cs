using System;

namespace Sorolla.PersistentData
{
    /// <summary>
    /// Events fired during save/load operations.
    /// </summary>
    public class SaveEvents
    {
        /// <summary>
        /// Fired before a save operation begins.
        /// Parameters: fileName, slot
        /// </summary>
        public event Action<string, int> OnBeforeSave;

        /// <summary>
        /// Fired after a save operation completes successfully.
        /// Parameters: fileName, slot
        /// </summary>
        public event Action<string, int> OnAfterSave;

        /// <summary>
        /// Fired before a load operation begins.
        /// Parameters: fileName, slot
        /// </summary>
        public event Action<string, int> OnBeforeLoad;

        /// <summary>
        /// Fired after a load operation completes successfully.
        /// Parameters: fileName, slot
        /// </summary>
        public event Action<string, int> OnAfterLoad;

        /// <summary>
        /// Fired when a save file is corrupted and defaults are used.
        /// Parameters: fileName, slot, exception
        /// </summary>
        public event Action<string, int, Exception> OnSaveCorrupted;

        /// <summary>
        /// Fired when a migration is applied.
        /// Parameters: fileName, slot, fromVersion, toVersion
        /// </summary>
        public event Action<string, int, int, int> OnMigrationApplied;

        internal void InvokeBeforeSave(string fileName, int slot) => OnBeforeSave?.Invoke(fileName, slot);
        internal void InvokeAfterSave(string fileName, int slot) => OnAfterSave?.Invoke(fileName, slot);
        internal void InvokeBeforeLoad(string fileName, int slot) => OnBeforeLoad?.Invoke(fileName, slot);
        internal void InvokeAfterLoad(string fileName, int slot) => OnAfterLoad?.Invoke(fileName, slot);
        internal void InvokeSaveCorrupted(string fileName, int slot, Exception ex) => OnSaveCorrupted?.Invoke(fileName, slot, ex);
        internal void InvokeMigrationApplied(string fileName, int slot, int from, int to) => OnMigrationApplied?.Invoke(fileName, slot, from, to);
    }
}
