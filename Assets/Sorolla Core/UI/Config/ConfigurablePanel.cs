using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sorolla.UI.Config
{
    /// <summary>
    /// Base class for config-driven panels.
    /// Automatically loads configuration based on the key passed in ShowAsync args.
    /// </summary>
    /// <typeparam name="TKey">The enum type used as the configuration key</typeparam>
    /// <typeparam name="TConfig">The configuration data type</typeparam>
    public abstract class ConfigurablePanel<TKey, TConfig> : UIPanel
        where TKey : Enum
        where TConfig : class
    {
        [Header("Configuration")]
        [SerializeField] protected PanelConfigBase<TKey, TConfig> _configAsset;

        protected TConfig _currentConfig;
        protected TKey _currentKey;

        /// <summary>
        /// The default key to use if none is provided in ShowAsync args.
        /// </summary>
        protected abstract TKey DefaultKey { get; }

        public override async UniTask ShowAsync(object args = null)
        {
            // Determine key from args or use default
            if (args is TKey key)
            {
                _currentKey = key;
            }
            else
            {
                _currentKey = DefaultKey;
            }

            // Load configuration
            _currentConfig = _configAsset != null ? _configAsset.GetConfig(_currentKey) : null;

            // Apply configuration
            ApplyConfig(_currentConfig);

            // Call base (activates and fires events)
            await base.ShowAsync(args);
        }

        /// <summary>
        /// Apply the loaded configuration to the panel UI.
        /// Must be implemented by subclasses to update their specific UI elements.
        /// </summary>
        protected abstract void ApplyConfig(TConfig config);

        /// <summary>
        /// Get the current configuration.
        /// </summary>
        public TConfig CurrentConfig => _currentConfig;

        /// <summary>
        /// Get the current key.
        /// </summary>
        public TKey CurrentKey => _currentKey;
    }
}
