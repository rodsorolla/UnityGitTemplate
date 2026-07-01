using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Sorolla.Tournaments;
using Sorolla.Tournaments.UI;
using Sorolla.UI;
using UnityEngine;

namespace Template
{
    /// <summary>
    /// Play-mode test harness for the agnostic Profile/Tournament UI. Drop it on any GameObject in
    /// the bootstrap scene, assign the refs, then — in Play mode — pick a surface from the dropdown
    /// and press <b>Open Selected UI</b> (styled with NaughtyAttributes, mirroring the UIManager
    /// inspector). Requires <see cref="TemplateBootstrap"/> in the scene so the Core services resolve.
    /// </summary>
    public class TemplateUITester : MonoBehaviour
    {
        /// <summary>The UI surfaces this harness can drive.</summary>
        private enum Surface
        {
            TournamentScreen,
            ProfilePanel,
            RankRevealStrip,
        }

        [BoxGroup("UI Tester")]
        [InfoBox("Enter Play mode, pick a surface, then press Open.", EInfoBoxType.Normal)]
        [Label("Surface")]
        [SerializeField] private Surface _surface = Surface.TournamentScreen;

        [Button("Open Selected UI", EButtonEnableMode.Playmode)]
        private void OpenSelected()
        {
            switch (_surface)
            {
                case Surface.TournamentScreen: ShowTournamentScreen(); break;
                case Surface.ProfilePanel:     OpenProfilePanel();     break;
                case Surface.RankRevealStrip:  PlayStripReveal();      break;
            }
        }

        [Button("Hide Selected UI", EButtonEnableMode.Playmode)]
        private void HideSelected()
        {
            switch (_surface)
            {
                case Surface.TournamentScreen: HideTournamentScreen(); break;
                case Surface.ProfilePanel:     HideProfilePanel();     break;
                case Surface.RankRevealStrip:  HideStrip();            break;
            }
        }

        [BoxGroup("References")]
        [Tooltip("The TournamentScreen prefab (Assets/_Template/Prefabs/UI/_Tournament).")]
        [SerializeField] private GameObject _tournamentScreenPrefab;

        [BoxGroup("References")]
        [Tooltip("Canvas (or child) to parent the screen under. Defaults to the first Canvas found.")]
        [SerializeField] private Transform _uiRoot;

        [BoxGroup("References")]
        [ShowIf(nameof(_surface), Surface.RankRevealStrip)]
        [Tooltip("An instance of a strip prefab you authored (carries TournamentRankRevealStrip). " +
                 "Stays inert until assigned.")]
        [SerializeField] private TournamentRankRevealStrip _strip;

        [BoxGroup("References")]
        [ShowIf(nameof(_surface), Surface.RankRevealStrip)]
        [SerializeField] private int _oldRank = 50;

        [BoxGroup("References")]
        [ShowIf(nameof(_surface), Surface.RankRevealStrip)]
        [SerializeField] private int _newRank = 10;

        private GameObject _screenInstance;

        public void ShowTournamentScreen()
        {
            if (!EnsurePlaying()) return;
            if (_tournamentScreenPrefab == null) { Debug.LogWarning("[Tester] Assign the TournamentScreen prefab."); return; }

            if (_screenInstance == null)
            {
                var parent = _uiRoot != null ? _uiRoot : FindAnyObjectByType<Canvas>()?.transform;
                if (parent == null) { Debug.LogWarning("[Tester] No Canvas found; assign UI Root."); return; }
                _screenInstance = Instantiate(_tournamentScreenPrefab, parent, false);
            }
            _screenInstance.SetActive(true);   // TournamentScreen rebuilds itself in OnEnable
        }

        public void HideTournamentScreen()
        {
            if (_screenInstance != null) _screenInstance.SetActive(false);
        }

        public void HideProfilePanel()
        {
            if (UIManager.Instance == null) { Debug.LogWarning("[Tester] No UIManager in scene."); return; }
            UIManager.Instance.ClosePanelsByIdAsync(UIPanelId.Profile).Forget();
        }

        public void HideStrip()
        {
            if (_strip != null) _strip.gameObject.SetActive(false);
        }

        public void OpenProfilePanel()
        {
            if (!EnsurePlaying()) return;
            if (UIManager.Instance == null) { Debug.LogWarning("[Tester] No UIManager in scene."); return; }
            UIManager.Instance.OpenPanelAsync(UIPanelId.Profile).Forget();
        }

        public void PlayStripReveal()
        {
            if (!EnsurePlaying()) return;
            if (_strip == null) { Debug.LogWarning("[Tester] Assign a strip instance (author the strip prefab first)."); return; }
            _strip.Play(new RankReveal { OldRank = _oldRank, NewRank = _newRank, Improved = _newRank < _oldRank });
        }

        private bool EnsurePlaying()
        {
            if (Application.isPlaying) return true;
            Debug.LogWarning("[Tester] Enter Play mode first (services + UIManager are runtime-only).");
            return false;
        }
    }
}
