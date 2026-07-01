using Cysharp.Threading.Tasks;
using Sorolla.Tournaments;
using Sorolla.Tournaments.UI;
using Sorolla.UI;
using UnityEngine;

namespace Template
{
    /// <summary>
    /// Play-mode test harness for the agnostic Profile/Tournament UI. Drop it on any GameObject in
    /// the bootstrap scene, assign the refs, and drive the three surfaces from the inspector's
    /// context menu (the "⋮" gear on the component header) or by hooking the public methods to
    /// UI buttons. Requires <see cref="TemplateBootstrap"/> in the scene so the Core services resolve.
    /// </summary>
    public class TemplateUITester : MonoBehaviour
    {
        [Header("Tournament screen")]
        [Tooltip("The TournamentScreen prefab (Assets/_Template/Prefabs/UI/_Tournament).")]
        [SerializeField] private GameObject _tournamentScreenPrefab;
        [Tooltip("Canvas (or child) to parent the screen under. Defaults to the first Canvas found.")]
        [SerializeField] private Transform _uiRoot;

        [Header("Rank-reveal strip")]
        [Tooltip("An instance of a strip prefab you authored (carries TournamentRankRevealStrip). " +
                 "Stays inert until assigned.")]
        [SerializeField] private TournamentRankRevealStrip _strip;
        [SerializeField] private int _oldRank = 50;
        [SerializeField] private int _newRank = 10;

        private GameObject _screenInstance;

        [ContextMenu("Show Tournament Screen")]
        public void ShowTournamentScreen()
        {
            if (!EnsurePlaying()) return;
            if (_tournamentScreenPrefab == null) { Debug.LogWarning("[Tester] Assign the TournamentScreen prefab."); return; }

            if (_screenInstance == null)
            {
                var parent = _uiRoot != null ? _uiRoot : FindFirstObjectByType<Canvas>()?.transform;
                if (parent == null) { Debug.LogWarning("[Tester] No Canvas found; assign UI Root."); return; }
                _screenInstance = Instantiate(_tournamentScreenPrefab, parent, false);
            }
            _screenInstance.SetActive(true);   // TournamentScreen rebuilds itself in OnEnable
        }

        [ContextMenu("Hide Tournament Screen")]
        public void HideTournamentScreen()
        {
            if (_screenInstance != null) _screenInstance.SetActive(false);
        }

        [ContextMenu("Open Profile Panel")]
        public void OpenProfilePanel()
        {
            if (!EnsurePlaying()) return;
            if (UIManager.Instance == null) { Debug.LogWarning("[Tester] No UIManager in scene."); return; }
            UIManager.Instance.OpenPanelAsync(UIPanelId.Profile).Forget();
        }

        [ContextMenu("Play Strip Reveal")]
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
