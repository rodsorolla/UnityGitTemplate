using System.Collections.Generic;
using Sorolla;
using Sorolla.Cosmetics;
using Sorolla.Profile;
using Sorolla.Tournaments;
using Sorolla.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tournaments.UI
{
    /// Leaderboard sub-screen — one of the horizontally-sliding main-menu screens
    /// (same pattern as WorldsScreen/ShopUI: plain MonoBehaviour, rebuilds in OnEnable).
    /// Top-3 are pinned (RankLine1/2/3); the scroll list holds ranks 4..N and
    /// auto-scrolls to centre the player when they rank below the podium.
    public class TournamentScreen : MonoBehaviour
    {
        [Header("Tier strip")]
        [Tooltip("One shine GO per tier cup, in tier order. Only the current tier's shine is enabled.")]
        [SerializeField] private GameObject[] _tierShines;

        [Header("Header")]
        [SerializeField] private TMP_Text _tierName;
        [SerializeField] private TMP_Text _timeRemaining;
        [SerializeField] private Image _headerCupIcon;
        [Tooltip("Cup sprite per tier, in tier order. Indexed by the current tier.")]
        [SerializeField] private Sprite[] _tierCupSprites;

        [Header("Header colours (per tier)")]
        [Tooltip("Header BG image — recoloured with the active tier's Color1.")]
        [SerializeField] private Image _headerBg;
        [Tooltip("Header Glow image — recoloured with the active tier's Color2.")]
        [SerializeField] private Image _headerGlow;
        [Tooltip("Header Glow2 image — recoloured with the active tier's Color2.")]
        [SerializeField] private Image _headerGlow2;
        [Tooltip("Header Shadow image — recoloured with the active tier's Color3.")]
        [SerializeField] private Image _headerShadow;
        [Tooltip("Colour set per tier, in tier order. Indexed by the current tier.")]
        [SerializeField] private TierHeaderColors[] _tierHeaderColors;

        [System.Serializable]
        private struct TierHeaderColors
        {
            [Tooltip("Color1 — applied to the header BG.")]
            public Color bg;
            [Tooltip("Color2 — applied to both header glows.")]
            public Color glow;
            [Tooltip("Color3 — applied to the header shadow.")]
            public Color shadow;
        }

        [Header("Pinned rows")]
        [Tooltip("Top-3 podium rows (RankLine1/2/3), in rank order.")]
        [SerializeField] private TournamentRowView[] _topRows;
        [Tooltip("Always-visible row showing the local player.")]
        [SerializeField] private TournamentRowView _playerRow;

        [Header("Scroll list (ranks 4..N)")]
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _rowsRoot;
        [SerializeField] private TournamentRowView _rowPrefab;

        [Header("Profile button (avatar only)")]
        [Tooltip("Header profile button — opens the Profile panel on tap. Optional.")]
        [SerializeField] private Button _profileButton;
        [Tooltip("Avatar image on the profile button. Shows the player's selected avatar. Optional.")]
        [SerializeField] private Image _profileAvatar;

        [SerializeField] private ProfileCatalog _catalog;

        [Header("Chest reward bubble")]
        [Tooltip("Shared popup shown over a podium chest when tapped. Optional.")]
        [SerializeField] private ChestRewardBubble _rewardBubble;

        [Header("Info button")]
        [Tooltip("ⓘ button — opens the Tournament info panel on tap. Optional.")]
        [SerializeField] private Button _infoButton;

        [Header("Skin tip")]
        [Tooltip("Shared bubble shown over a cup when tapped, revealing the skin that unlocks at that tier. Optional.")]
        [SerializeField] private SkinTipView _skinTip;
        [Tooltip("Cup button <-> skin pairs. Only tiers that unlock a skin (Silver/Sapphire/Ruby) need an entry.")]
        [SerializeField] private CupSkinBinding[] _cupSkins;

        [System.Serializable]
        private struct CupSkinBinding
        {
            [Tooltip("Button on the cup tier icon.")]
            public Button cupButton;
            [Tooltip("Skin that unlocks at this tier — its icon is shown in the tip.")]
            public SkinDefinition skin;
        }

        private ITournamentService _service;
        private IPlayerProfile _profile;
        private IIconResolver _icons;
        private readonly List<TournamentRowView> _spawned = new List<TournamentRowView>();
        private Coroutine _centerRoutine;

        private float _tick;
        private int _lastShownMinutes = -1;

        private void Awake()
        {
            if (_profileButton != null)
                _profileButton.onClick.AddListener(OpenProfile);

            if (_infoButton != null)
                _infoButton.onClick.AddListener(OpenInfo);

            // Podium chests: subscribe once (rows are fixed serialized refs).
            if (_topRows != null)
                foreach (var row in _topRows)
                    if (row != null) row.OnChestClicked += ShowRewardBubble;

            // Cup tier icons: tap to reveal the skin that unlocks at that tier.
            if (_cupSkins != null)
                foreach (var binding in _cupSkins)
                {
                    if (binding.cupButton == null) continue;
                    var b = binding; // capture a copy for the closure
                    b.cupButton.onClick.AddListener(() => ShowSkinTip(b));
                }
        }

        // Rebuilds whenever the screen becomes active — i.e. when the menu (re)appears,
        // matching WorldsScreen/ShopUI. The GameObject is active here, so the auto-scroll
        // layout math is valid even while this screen is slid off-screen.
        private void OnEnable()
        {
            _service = ServiceLocator.Instance.TryResolve<ITournamentService>();
            _profile = ServiceLocator.Instance.TryResolve<IPlayerProfile>();
            _icons = ServiceLocator.Instance.TryResolve<IIconResolver>();

            // Keep the header avatar in sync if the player changes it in the Profile panel
            // (opened from this screen) — the screen stays active behind the overlay, so
            // OnEnable won't re-fire on its own.
            if (_profile != null)
            {
                _profile.OnProfileChanged -= HandleProfileChanged;
                _profile.OnProfileChanged += HandleProfileChanged;
            }
            RefreshProfileAvatar();

            Rebuild();
        }

        private void Rebuild()
        {
            ClearRows();
            if (_rewardBubble != null) _rewardBubble.Hide(); // close any stale bubble on re-open
            if (_skinTip != null) _skinTip.Hide();           // close any stale skin tip on re-open
            if (_service == null) return;

            var board = _service.GetLeaderboard();
            if (board == null) return;

            UpdateTierStrip();
            UpdateHeader();
            BindTopRows(board);
            BindPlayerRow(board);
            BuildScrollList(board);
        }

        private void UpdateTierStrip()
        {
            if (_tierShines == null) return;
            int active = _service.CurrentTierIndex;
            for (int i = 0; i < _tierShines.Length; i++)
                if (_tierShines[i] != null) _tierShines[i].SetActive(i == active);
        }

        private void UpdateHeader()
        {
            int idx = _service.CurrentTierIndex;
            var tiers = _service.Tiers;

            if (_tierName != null)
                _tierName.text = (tiers != null && idx >= 0 && idx < tiers.Count)
                    ? tiers[idx].name
                    : "Tier " + (idx + 1);

            if (_headerCupIcon != null && _tierCupSprites != null && idx >= 0 && idx < _tierCupSprites.Length)
                _headerCupIcon.sprite = _tierCupSprites[idx];

            UpdateHeaderColors(idx);

            _lastShownMinutes = -1; // force the countdown text to refresh this open
            UpdateCountdown();
        }

        // Recolour the header backdrop to the active tier's palette.
        private void UpdateHeaderColors(int idx)
        {
            if (_tierHeaderColors == null || idx < 0 || idx >= _tierHeaderColors.Length) return;
            var c = _tierHeaderColors[idx];

            if (_headerBg != null) _headerBg.color = c.bg;
            if (_headerGlow != null) _headerGlow.color = c.glow;
            if (_headerGlow2 != null) _headerGlow2.color = c.glow;
            if (_headerShadow != null) _headerShadow.color = c.shadow;
        }

        private void BindTopRows(IReadOnlyList<LeaderboardEntry> board)
        {
            if (_topRows == null) return;
            for (int i = 0; i < _topRows.Length; i++)
            {
                var row = _topRows[i];
                if (row == null) continue;
                if (i < board.Count)
                {
                    row.gameObject.SetActive(true);
                    row.Bind(board[i], _catalog);
                }
                else
                {
                    row.gameObject.SetActive(false);
                }
            }
        }

        private void BindPlayerRow(IReadOnlyList<LeaderboardEntry> board)
        {
            if (_playerRow == null) return;
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i].IsPlayer)
                {
                    _playerRow.gameObject.SetActive(true);
                    _playerRow.Bind(board[i], _catalog);
                    return;
                }
            }
            _playerRow.gameObject.SetActive(false);
        }

        private void BuildScrollList(IReadOnlyList<LeaderboardEntry> board)
        {
            if (_rowsRoot == null || _rowPrefab == null) return;

            RectTransform playerRowRect = null;
            for (int i = 3; i < board.Count; i++)
            {
                var row = Instantiate(_rowPrefab, _rowsRoot);
                row.Bind(board[i], _catalog);
                _spawned.Add(row);
                if (board[i].IsPlayer) playerRowRect = (RectTransform)row.transform;
            }

            if (playerRowRect != null)
            {
                // Defer centering: on the frame the screen first activates the content layout
                // hasn't run yet (content height measures 0), so an immediate scroll is discarded.
                // OnEnable only fires once (the screen stays active, just slid off-screen), so we
                // must wait for the layout pass rather than rely on a later rebuild.
                if (_centerRoutine != null) StopCoroutine(_centerRoutine);
                _centerRoutine = StartCoroutine(CenterOnDeferred(playerRowRect));
            }
        }

        private System.Collections.IEnumerator CenterOnDeferred(RectTransform row)
        {
            yield return new WaitForEndOfFrame();   // let the canvas finish this frame's layout pass
            Canvas.ForceUpdateCanvases();
            _centerRoutine = null;
            if (row != null) CenterOn(row);
        }

        /// Scrolls the list so the given row sits in the viewport centre.
        private void CenterOn(RectTransform row)
        {
            if (_scroll == null || _rowsRoot == null || _scroll.viewport == null) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsRoot);

            float viewportH = _scroll.viewport.rect.height;
            float contentH = _rowsRoot.rect.height;
            float maxScroll = contentH - viewportH;
            Debug.Log($"[TournamentScreen.CenterOn] viewportH={viewportH} contentH={contentH} maxScroll={maxScroll} rowY={row.anchoredPosition.y} rowH={row.rect.height} activeInHierarchy={gameObject.activeInHierarchy}");
            if (maxScroll <= 0f) return; // everything fits — no scroll needed

            float rowTop = -row.anchoredPosition.y; // content-top → row-top distance
            float target = rowTop - (viewportH - row.rect.height) * 0.5f;
            target = Mathf.Clamp(target, 0f, maxScroll);
            _scroll.verticalNormalizedPosition = 1f - target / maxScroll;
            Debug.Log($"[TournamentScreen.CenterOn] rowTop={rowTop} target={target} -> vNormPos={_scroll.verticalNormalizedPosition}");
        }

        private void Update()
        {
            if (_service == null || _timeRemaining == null) return;
            _tick += Time.unscaledDeltaTime; // menu may run at timeScale 0
            if (_tick < 1f) return;
            _tick = 0f;
            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (_timeRemaining == null) return;
            var remaining = _service.WeekEndUtc - System.DateTime.UtcNow;
            if (remaining < System.TimeSpan.Zero) remaining = System.TimeSpan.Zero;

            int totalMinutes = (int)remaining.TotalMinutes;
            if (totalMinutes == _lastShownMinutes) return; // avoid per-second string allocs
            _lastShownMinutes = totalMinutes;
            _timeRemaining.text = Format(remaining);
        }

        private static string Format(System.TimeSpan t)
        {
            if (t.Days > 0) return $"{t.Days}d {t.Hours}h";
            if (t.Hours > 0) return $"{t.Hours}h {t.Minutes}m";
            return $"{t.Minutes}m";
        }

        private void ClearRows()
        {
            foreach (var r in _spawned)
                if (r != null) Destroy(r.gameObject);
            _spawned.Clear();
        }

        // Sets the profile button's avatar to the player's selected one, falling back to the
        // catalog's first (seeded default). Null-guarded so prefabs without the button/catalog
        // wired stay safe. Avatar only — no flag, per design.
        private void RefreshProfileAvatar()
        {
            if (_profileAvatar == null || _catalog == null) return;

            var sprite = _catalog.GetAvatarSprite(_profile?.AvatarId);
            if (sprite == null)
                sprite = _catalog.GetAvatarSprite(_catalog.FirstAvatarId);

            _profileAvatar.sprite = sprite;
        }

        private void OpenProfile()
        {
            _ = UIManager.Instance?.OpenPanelAsync(UIPanelId.Profile);
        }

        private void OpenInfo()
        {
            _ = UIManager.Instance?.OpenPanelAsync(UIPanelId.TournamentInfo);
        }

        // Podium chest tapped: show that rank's rewards (current tier) over the chest.
        // Only ranks 1-3 have reward data; rows without rewards are ignored.
        private void ShowRewardBubble(TournamentRowView row)
        {
            if (_rewardBubble == null || _service == null || row == null) return;

            var tiers = _service.Tiers;
            int idx = _service.CurrentTierIndex;
            if (tiers == null || idx < 0 || idx >= tiers.Count) return;

            var rewards = tiers[idx].PodiumReward(row.Rank);
            if (rewards == null || rewards.Length == 0) return;

            _rewardBubble.Show(row.ChestAnchor, rewards, _icons);
        }

        // Cup tier tapped: pop the skin tip over that cup, showing the skin that unlocks there.
        private void ShowSkinTip(CupSkinBinding binding)
        {
            if (_skinTip == null || binding.cupButton == null || binding.skin == null) return;
            _skinTip.ShowFor((RectTransform)binding.cupButton.transform, binding.skin.Icon);
        }

        // Avatar saved in the Profile panel: refresh the header avatar AND re-bind the pinned
        // player row so its avatar updates immediately (no full leaderboard rebuild / scroll reset).
        private void HandleProfileChanged()
        {
            RefreshProfileAvatar();
            if (_playerRow != null && _service != null)
                BindPlayerRow(_service.GetLeaderboard());
        }

        private void OnDisable()
        {
            if (_profile != null)
                _profile.OnProfileChanged -= HandleProfileChanged;
            if (_rewardBubble != null) _rewardBubble.Hide();
            if (_skinTip != null) _skinTip.Hide();
            ClearRows();
        }

        private void OnDestroy()
        {
            if (_topRows != null)
                foreach (var row in _topRows)
                    if (row != null) row.OnChestClicked -= ShowRewardBubble;

            // Cup buttons are dedicated to this screen; clear their per-binding click closures.
            if (_cupSkins != null)
                foreach (var binding in _cupSkins)
                    if (binding.cupButton != null) binding.cupButton.onClick.RemoveAllListeners();
        }
    }
}
