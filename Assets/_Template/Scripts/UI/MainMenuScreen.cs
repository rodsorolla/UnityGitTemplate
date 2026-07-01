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

        private void Awake()
        {
            if (_playButton != null) _playButton.onClick.AddListener(OnPlay);
            if (_profileButton != null) _profileButton.onClick.AddListener(OnProfile);
            if (_tournamentButton != null) _tournamentButton.onClick.AddListener(OnTournament);
        }

        private void OnPlay()
        {
            var session = Object.FindFirstObjectByType<LevelSessionController>();
            session?.RequestStartLevel();
        }

        private void OnProfile()
        {
            UIManager.Instance.OpenPanelAsync(UIPanelId.Profile).Forget();
        }

        private void OnTournament()
        {
            var host = Object.FindFirstObjectByType<TemplateTournamentHost>(FindObjectsInactive.Include);
            host?.Show();
        }
    }
}
