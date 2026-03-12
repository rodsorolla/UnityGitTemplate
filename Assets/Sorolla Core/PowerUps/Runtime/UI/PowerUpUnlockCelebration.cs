using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.PowerUps
{
    /// <summary>
    /// Shows a one-time celebration message when a power-up is unlocked.
    /// Place on a screen that shows between levels (e.g. main menu).
    /// On enable, checks for pending celebrations and shows the first one found.
    /// </summary>
    public class PowerUpUnlockCelebration : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _celebrationRoot;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Image _powerUpIcon;
        [SerializeField] private Button _dismissButton;

        [Header("Settings")]
        [Tooltip("Message format. {0} = power-up display name")]
        [SerializeField] private string _messageFormat = "Congratulations! You've unlocked {0}!";

        private IPowerUpService _service;
        private PowerUpId _currentPowerUpId;

        private void Start()
        {
            if (_dismissButton != null)
                _dismissButton.onClick.AddListener(Dismiss);
        }

        private void OnEnable()
        {
            _service ??= ServiceLocator.Instance.TryResolve<IPowerUpService>();
            if (_service == null)
            {
                Debug.Log("[PowerUpUnlockCelebration] Service not available");
                return;
            }

            var pending = _service.GetNextPendingCelebration();
            Debug.Log($"[PowerUpUnlockCelebration] OnEnable - pending={pending?.DisplayName ?? "none"}");

            if (pending != null)
            {
                Show(pending);
            }
            else
            {
                Hide();
            }
        }

        private void Show(PowerUpDefinitionBase definition)
        {
            _currentPowerUpId = definition.PowerUpId;

            if (_messageText != null)
                _messageText.text = string.Format(_messageFormat, definition.DisplayName);

            if (_powerUpIcon != null)
                _powerUpIcon.sprite = definition.Icon;

            if (_celebrationRoot != null)
                _celebrationRoot.SetActive(true);
        }

        private void Hide()
        {
            if (_celebrationRoot != null)
                _celebrationRoot.SetActive(false);
        }

        private void Dismiss()
        {
            _service?.MarkUnlockCelebrationSeen(_currentPowerUpId);
            Hide();
        }

        private void OnDestroy()
        {
            if (_dismissButton != null)
                _dismissButton.onClick.RemoveListener(Dismiss);
        }
    }
}
