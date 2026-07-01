using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Sorolla.UI;
using Sorolla.LevelFlow;

namespace Template
{
    /// <summary>
    /// Agnostic main menu. PLAY drives the Core LevelSessionController; PROFILE opens the
    /// Profile panel; TOURNAMENT toggles the scene-hosted TournamentScreen.
    /// </summary>
    public class MainMenuScreen : UIScreen
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _profileButton;
        [SerializeField] private Button _tournamentButton;
        [SerializeField] private Button _shopButton;

        private void Awake()
        {
            if (_playButton != null) _playButton.onClick.AddListener(OnPlay);
            if (_profileButton != null) _profileButton.onClick.AddListener(OnProfile);
            if (_tournamentButton != null) _tournamentButton.onClick.AddListener(OnTournament);
            if (_shopButton != null) _shopButton.onClick.AddListener(OnShop);
        }

        private void OnPlay()
        {
            var session = Object.FindFirstObjectByType<LevelSessionController>();
            if (session == null) { Debug.LogWarning("[MainMenuScreen] No LevelSessionController in the scene; Play does nothing."); return; }
            session.RequestStartLevel();
        }

        private void OnProfile()
        {
            UIManager.Instance.OpenPanelAsync(UIPanelId.Profile).Forget();
        }

        private void OnTournament()
        {
            var host = Object.FindFirstObjectByType<TemplateTournamentHost>(FindObjectsInactive.Include);
            if (host == null) { Debug.LogWarning("[MainMenuScreen] No TemplateTournamentHost in the scene; Tournament does nothing."); return; }
            host.Show();
        }

        private void OnShop()
        {
            UIManager.Instance.PushScreenAsync(UIScreenId.Shop).Forget();
        }
    }
}
