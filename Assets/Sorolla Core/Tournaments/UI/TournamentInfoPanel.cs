using Cysharp.Threading.Tasks;
using Sorolla.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tournaments.UI
{
    /// <summary>
    /// Static explanation screen for the Tournament feature.
    /// Auto-opens once the first time the tournament unlocks (see <see cref="HomeUI"/>),
    /// and on demand from the ⓘ button on the Tournament screen (<see cref="TournamentScreen"/>).
    /// Purely informational — both Close and "Tap to Continue" simply dismiss it.
    /// </summary>
    public class TournamentInfoPanel : UIPanel
    {
        [Header("Buttons")]
        [Tooltip("Full-screen 'Tap to Continue' button. Optional.")]
        [SerializeField] private Button _continueButton;

        private UIManager _uiManager;

        private void OnEnable()
        {
            _uiManager = UIManager.Instance;
            if (_continueButton != null) _continueButton.onClick.AddListener(HandleClose);
        }

        private void OnDisable()
        {
            if (_continueButton != null) _continueButton.onClick.RemoveListener(HandleClose);
        }

        private async void HandleClose()
        {
            if (_uiManager != null)
                await _uiManager.ClosePanelsByIdAsync(UIPanelId.TournamentInfo);
            else
                await HideAsync();
        }
    }
}
