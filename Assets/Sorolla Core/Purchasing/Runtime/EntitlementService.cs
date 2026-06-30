using System;
using System.Collections.Generic;
using Sorolla.PersistentData;
using UnityEngine;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// Persistent set of granted entitlement keys. Saved via SaveSystem (batched —
    /// flushed on app pause/focus/quit so we don't pay per-frame disk I/O on iOS).
    /// On Initialize, runs a one-shot migration from the legacy `iap.json` file
    /// (HasNoAds = true → "noads" entitlement) and deletes the legacy file.
    /// </summary>
    public class EntitlementService : SorollaManager
    {
        public const string DefaultSaveFile = "entitlements";
        public const string DefaultLegacyFile = "iap";
        public const string NoAdsKey = "noads";

        private string _saveFile = DefaultSaveFile;
        private string _legacyFile = DefaultLegacyFile;
        // Initialized so Grant/Revoke/Has are safe if called before Initialize() loads from disk.
        private EntitlementsSaveData _data = new EntitlementsSaveData();

        public event Action<string, bool> OnEntitlementChanged;

        public bool Has(string entitlementKey)
        {
            if (string.IsNullOrEmpty(entitlementKey)) return false;
            return _data != null && _data.Entitlements.Contains(entitlementKey);
        }

        public void Grant(string entitlementKey)
        {
            if (string.IsNullOrEmpty(entitlementKey)) return;
            if (_data.Entitlements.Contains(entitlementKey)) return;
            _data.Entitlements.Add(entitlementKey);
            SaveSystem.Save(_data, _saveFile);
            OnEntitlementChanged?.Invoke(entitlementKey, true);
        }

        public void Revoke(string entitlementKey)
        {
            if (string.IsNullOrEmpty(entitlementKey)) return;
            if (!_data.Entitlements.Remove(entitlementKey)) return;
            SaveSystem.Save(_data, _saveFile);
            OnEntitlementChanged?.Invoke(entitlementKey, false);
        }

        public IReadOnlyList<string> AllGranted => _data?.Entitlements ?? new List<string>();

        protected override void Initialize()
        {
            _data = SaveSystem.Load<EntitlementsSaveData>(_saveFile);
            MigrateLegacyIfNeeded();
            ServiceLocator.Instance.Register(this);
        }

        private void MigrateLegacyIfNeeded()
        {
            if (SaveSystem.Exists(_saveFile)) return;            // already migrated
            if (!SaveSystem.Exists(_legacyFile)) return;          // nothing to migrate

            var legacy = SaveSystem.Load<LegacyIapShape>(_legacyFile);
            if (legacy != null && legacy.HasNoAds)
            {
                _data.Entitlements.Add(NoAdsKey);
                SaveSystem.Save(_data, _saveFile);
                Debug.Log("[EntitlementService] Migrated legacy iap.json HasNoAds → entitlements.json noads.");
            }
            SaveSystem.Delete(_legacyFile);
        }

        // Internal stub matching the deleted IAPSaveData schema, used only to read the legacy
        // file during migration. Newtonsoft will populate HasNoAds; Version is round-tripped
        // by SaveSystem but we don't read it.
        [Serializable]
        private class LegacyIapShape : ISaveData
        {
            public int Version => 1;
            public bool HasNoAds;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Test seam — constructs a hidden GameObject with the service attached.</summary>
        public static EntitlementService CreateForTests(string saveFile, string legacyFile)
        {
            var go = new GameObject("EntitlementService_Test") { hideFlags = HideFlags.HideAndDontSave };
            var svc = go.AddComponent<EntitlementService>();
            svc._saveFile = saveFile;
            svc._legacyFile = legacyFile;
            svc.Init();
            return svc;
        }
#endif
    }
}
