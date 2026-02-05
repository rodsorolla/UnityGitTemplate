using Sorolla.PersistentData;
using UnityEngine;

namespace Sorolla.FTX
{
    /// <summary>
    /// Service for managing first-time hints and experiences.
    /// Uses SaveSystem for persistence (ftx.json file).
    /// </summary>
    public class FirstTimeExperienceService : SorollaManager, IFirstTimeExperienceService
    {
        private const string SaveFileName = "ftx";

        private FirstTimeExperienceData _data;
        private bool _isDirty;

        protected override void Initialize()
        {
            Load();
            ServiceLocator.Instance.Register<IFirstTimeExperienceService>(this);
        }

        private void OnDestroy()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _isDirty)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        #region IFirstTimeExperienceService Implementation

        public bool HasSeen(string key)
        {
            return _data.HasSeen(key);
        }

        public void MarkAsSeen(string key)
        {
            if (_data.MarkAsSeen(key))
            {
                _isDirty = true;
            }
        }

        public bool CheckFirstTime(string key)
        {
            if (_data.HasSeen(key))
                return false;

            _data.MarkAsSeen(key);
            _isDirty = true;
            return true;
        }

        #endregion

        #region Persistence

        public void Save()
        {
            var result = SaveSystem.Save(_data, SaveFileName);
            if (result.Success)
            {
                _isDirty = false;
            }
            else
            {
                Debug.LogError($"[FirstTimeExperienceService] Save failed: {result.ErrorMessage}");
            }
        }

        public void Load()
        {
            _data = SaveSystem.Load<FirstTimeExperienceData>(SaveFileName);
            _isDirty = false;
        }

        #endregion

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// [DEBUG] Gets all seen keys for editor display.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<string> DEBUG_GetAllSeenKeys()
        {
            return _data?.SeenKeys;
        }

        /// <summary>
        /// [DEBUG] Resets a specific key.
        /// </summary>
        public void DEBUG_ResetKey(string key)
        {
            if (_data == null) return;
            if (_data.ResetKey(key))
            {
                _isDirty = true;
                Debug.Log($"[FirstTimeExperienceService] DEBUG: Reset key: {key}");
            }
        }

        /// <summary>
        /// [DEBUG] Resets all seen keys.
        /// </summary>
        public void DEBUG_ResetAll()
        {
            if (_data == null) return;
            _data.ResetAll();
            _isDirty = true;
            Debug.Log("[FirstTimeExperienceService] DEBUG: Reset all keys");
        }
#endif
    }
}
