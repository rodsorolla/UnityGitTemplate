using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.UI.Config
{
    /// <summary>
    /// Base class for enum-keyed panel configurations.
    /// Subclasses define the key enum type and config data structure.
    /// </summary>
    /// <typeparam name="TKey">The enum type used as the lookup key</typeparam>
    /// <typeparam name="TConfig">The configuration data type</typeparam>
    public abstract class PanelConfigBase<TKey, TConfig> : ScriptableObject
        where TKey : Enum
        where TConfig : class
    {
        [SerializeField] protected List<ConfigEntry> _configs = new();
        [SerializeField] protected TConfig _defaultConfig;

        private Dictionary<TKey, TConfig> _map;

        /// <summary>
        /// Entry mapping a key to a configuration.
        /// </summary>
        [Serializable]
        public class ConfigEntry
        {
            public TKey key;
            public TConfig config;
        }

        protected virtual void OnEnable()
        {
            RebuildMap();
        }

        protected void RebuildMap()
        {
            _map = new Dictionary<TKey, TConfig>();
            foreach (var entry in _configs)
            {
                if (entry?.config != null)
                {
                    _map[entry.key] = entry.config;
                }
            }
        }

        private void EnsureMapInitialized()
        {
            if (_map == null)
                RebuildMap();
        }

        /// <summary>
        /// Get the configuration for a specific key.
        /// Returns the default config if no specific config exists.
        /// </summary>
        public TConfig GetConfig(TKey key)
        {
            EnsureMapInitialized();
            return _map.TryGetValue(key, out var config) ? config : _defaultConfig;
        }

        /// <summary>
        /// Check if a specific key has a configuration.
        /// </summary>
        public bool HasConfig(TKey key)
        {
            EnsureMapInitialized();
            return _map.ContainsKey(key);
        }

        /// <summary>
        /// Get all configured keys.
        /// </summary>
        public IEnumerable<TKey> GetConfiguredKeys()
        {
            EnsureMapInitialized();
            return _map.Keys;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            RebuildMap();
        }
#endif
    }
}
