using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.PowerUps
{
    /// <summary>
    /// Spawns one PowerUpButtonUI per registry entry and keeps them in sync with the service.
    /// Game code subscribes to OnPowerUpButtonPressed to handle power-up logic.
    /// Call Init() after services are ready.
    /// </summary>
    public class PowerUpBar : MonoBehaviour
    {
        [SerializeField] private PowerUpRegistry _registry;
        [SerializeField] private Transform _container;
        [SerializeField] private PowerUpButtonUI _buttonPrefab;

        /// <summary>
        /// Fired when any power-up button is tapped.
        /// </summary>
        public event Action<PowerUpId> OnPowerUpButtonPressed;

        private readonly Dictionary<PowerUpId, PowerUpButtonUI> _buttons = new();
        private readonly Dictionary<PowerUpId, bool> _externalCanUse = new();
        private readonly HashSet<PowerUpId> _suppressedAutoHints = new();
        private IPowerUpService _service;

        /// <summary>
        /// Call after services are registered. Spawns buttons and subscribes to events.
        /// </summary>
        public void Init()
        {
            _service = ServiceLocator.Instance.Resolve<IPowerUpService>();

            foreach (var def in _registry.PowerUps)
            {
                var button = Instantiate(_buttonPrefab, _container);
                button.Setup(def, HandleButtonPressed);
                _buttons[def.PowerUpId] = button;
            }

            RefreshAll();
            ShowPendingUnlockHints();

            _service.OnQuantityChanged += HandleQuantityChanged;
            _service.OnPowerUpUnlocked += HandlePowerUpUnlocked;
            _service.OnPowerUpUsed += HandlePowerUpUsed;
        }

        /// <summary>
        /// Refreshes a single button's visual state from the service.
        /// </summary>
        public void RefreshButton(PowerUpId id)
        {
            if (!_buttons.TryGetValue(id, out var button)) return;

            bool isUnlocked = _service.IsUnlocked(id);
            bool isFirstFree = _service.IsFirstUseFree(id);
            int quantity = _service.GetQuantity(id);
            bool canUse = _service.CanUse(id);

            // Apply external override if set
            if (_externalCanUse.TryGetValue(id, out bool externalAllow))
                canUse = canUse && externalAllow;

            button.UpdateState(isUnlocked, isFirstFree, quantity, canUse);
        }

        /// <summary>
        /// Refreshes every button.
        /// </summary>
        public void RefreshAll()
        {
            foreach (var id in _buttons.Keys)
                RefreshButton(id);
        }

        /// <summary>
        /// Allows game code to externally disable a button (e.g. CanUndo is false).
        /// Pass true to re-enable normal service-based logic.
        /// </summary>
        public void SetCanUse(PowerUpId id, bool canUse)
        {
            _externalCanUse[id] = canUse;
            RefreshButton(id);
        }

        /// <summary>
        /// Suppresses the automatic unlock hint for a power-up.
        /// Game code can then show it manually via ShowHintForPowerUp when ready.
        /// Call before Init() or before the power-up unlocks.
        /// </summary>
        public void SuppressAutoHint(PowerUpId id)
        {
            _suppressedAutoHints.Add(id);
        }

        /// <summary>
        /// Manually shows the unlock hint for a power-up if the notification hasn't been seen yet.
        /// </summary>
        public void ShowHintForPowerUp(PowerUpId id)
        {
            if (_service.HasSeenUnlockNotification(id)) return;
            if (!_buttons.TryGetValue(id, out var button)) return;

            var definition = _service.GetDefinition(id);
            if (definition == null) return;

            button.ShowUnlockHint(definition.Description);
        }

        private void ShowPendingUnlockHints()
        {
            foreach (var kvp in _buttons)
            {
                var id = kvp.Key;
                if (_suppressedAutoHints.Contains(id)) continue;
                if (!_service.IsUnlocked(id)) continue;
                if (_service.HasSeenUnlockNotification(id)) continue;

                var definition = _service.GetDefinition(id);
                if (definition == null) continue;

                kvp.Value.ShowUnlockHint(definition.Description);
            }
        }

        private void HandleButtonPressed(PowerUpId id)
        {
            // If the hint was showing, mark the unlock notification as seen
            if (_buttons.TryGetValue(id, out var button) && button.HideUnlockHint())
            {
                _service.MarkUnlockNotificationSeen(id);
            }

            OnPowerUpButtonPressed?.Invoke(id);
        }

        private void HandleQuantityChanged(PowerUpQuantityChangedEventArgs args)
        {
            RefreshButton(args.PowerUpId);
        }

        private void HandlePowerUpUnlocked(PowerUpUnlockedEventArgs args)
        {
            RefreshAll();

            // Show the unlock hint unless game code suppressed it
            if (_suppressedAutoHints.Contains(args.PowerUpId)) return;
            if (_buttons.TryGetValue(args.PowerUpId, out var button))
            {
                button.ShowUnlockHint(args.Definition.Description);
            }
        }

        private void HandlePowerUpUsed(PowerUpId id)
        {
            RefreshButton(id);
        }

        private void OnDestroy()
        {
            if (_service == null) return;
            _service.OnQuantityChanged -= HandleQuantityChanged;
            _service.OnPowerUpUnlocked -= HandlePowerUpUnlocked;
            _service.OnPowerUpUsed -= HandlePowerUpUsed;
        }
    }
}
