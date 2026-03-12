using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.PowerUps
{
    /// <summary>
    /// Individual power-up button. Lives on the button prefab.
    /// Call Setup() once, then UpdateState() whenever service state changes.
    /// </summary>
    public class PowerUpButtonUI : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _lockOverlay;
        [SerializeField] private TextMeshProUGUI _quantityText;
        [SerializeField] private GameObject _freeLabel;

        [Header("Unlock Hint")]
        [Tooltip("Optional hint GO shown once when the power-up first unlocks")]
        [SerializeField] private GameObject _unlockHint;
        [SerializeField] private TextMeshProUGUI _hintDescription;

        private PowerUpId _powerUpId;
        private Action<PowerUpId> _onClick;
        private bool _hintShowing;

        public PowerUpId PowerUpId => _powerUpId;

        /// <summary>
        /// One-time setup. Sets icon and wires the click callback.
        /// </summary>
        public void Setup(PowerUpDefinitionBase def, Action<PowerUpId> onClick)
        {
            _powerUpId = def.PowerUpId;
            _onClick = onClick;

            if (_icon != null)
                _icon.sprite = def.Icon;

            _button.onClick.AddListener(HandleClick);
        }

        /// <summary>
        /// Updates visual state based on current service data.
        /// </summary>
        public void UpdateState(bool isUnlocked, bool isFirstFree, int quantity, bool canUse)
        {
            if (!isUnlocked)
            {
                // Locked state — hide icon and quantity, show lock overlay
                if (_lockOverlay != null) _lockOverlay.SetActive(true);
                if (_icon != null) _icon.gameObject.SetActive(false);
                if (_freeLabel != null) _freeLabel.SetActive(false);
                if (_quantityText != null) _quantityText.transform.parent.gameObject.SetActive(false);
                _button.interactable = false;
                return;
            }

            if (_lockOverlay != null) _lockOverlay.SetActive(false);
            if (_icon != null) _icon.gameObject.SetActive(true);

            if (isFirstFree)
            {
                // Free use available — hide quantity parent, show free label
                if (_freeLabel != null) _freeLabel.SetActive(true);
                if (_quantityText != null) _quantityText.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                // Normal state — show quantity parent, hide free label
                if (_freeLabel != null) _freeLabel.SetActive(false);
                if (_quantityText != null)
                {
                    _quantityText.transform.parent.gameObject.SetActive(true);
                    _quantityText.text = quantity.ToString();
                }
            }

            _button.interactable = canUse;
        }

        /// <summary>
        /// Shows the unlock hint with the given description. Called by PowerUpBar.
        /// </summary>
        public void ShowUnlockHint(string description)
        {
            if (_unlockHint == null) return;

            _unlockHint.SetActive(true);
            _hintShowing = true;

            if (_hintDescription != null)
                _hintDescription.text = description;
        }

        /// <summary>
        /// Hides the unlock hint if showing.
        /// </summary>
        /// <returns>True if the hint was showing and got hidden.</returns>
        public bool HideUnlockHint()
        {
            if (!_hintShowing) return false;

            _hintShowing = false;
            if (_unlockHint != null) _unlockHint.SetActive(false);
            return true;
        }

        private void HandleClick()
        {
            _onClick?.Invoke(_powerUpId);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }
}
