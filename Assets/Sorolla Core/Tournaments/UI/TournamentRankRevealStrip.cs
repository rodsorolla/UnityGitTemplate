using System.Collections.Generic;
using DG.Tweening;
using Sorolla;
using Sorolla.Profile;
using Sorolla.Tournaments;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Sorolla.Tournaments.UI
{
    /// <summary>
    /// Horizontal rank-reveal strip on the Level Complete screen. A two-layer "rail" in pure local
    /// space — no ScrollRect, no LayoutGroup, no world-space math:
    ///  - _field    : masked layer holding ONLY the bot cards, positioned at slot*step and slid
    ///                horizontally to reveal the climb.
    ///  - _playerPin : unmasked node fixed at the viewport centre holding ONLY the player card; it
    ///                never moves horizontally, it only scales (pop up / pop in).
    ///
    /// Reveal beats: (1) player pops up, (2) the field slides so the player ends over its new
    /// position, (3) player pops in. The resting state is always slot*step with the field at 0, so
    /// no half-built/frozen state is possible. If the tournament is locked/unavailable, it hides.
    ///
    /// Prefab conventions (authored by the user):
    ///  - _viewport  : RectTransform + RectMask2D, fixed width (clips the field).
    ///  - _field     : child of _viewport, pivot/anchor centred (0.5, 0.5); NO HorizontalLayoutGroup,
    ///                 ContentSizeFitter or ScrollRect (the code also disables them defensively).
    ///  - _playerPin : UNMASKED node centred over the viewport (so the pop-up isn't clipped),
    ///                 pivot/anchor centred.
    ///  - card prefab: pivot (0.5, 0.5); carries a TournamentRowView.
    /// </summary>
    public class TournamentRankRevealStrip : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform _viewport;
        [FormerlySerializedAs("_content")]
        [SerializeField] private RectTransform _field;
        [FormerlySerializedAs("_playerOverlay")]
        [SerializeField] private RectTransform _playerPin;
        [SerializeField] private TournamentRowView _cardPrefab;
        [SerializeField] private ProfileCatalog _catalog;
        [Tooltip("Shows the time left until the weekly tournament resets (e.g. \"6d 12h\").")]
        [SerializeField] private TMP_Text _countdownText;

        [Header("Window")]
        [Tooltip("Bot cards shown on each side of the player.")]
        [Min(1)] [SerializeField] private int _visibleRadius = 3;
        [Tooltip("Card width in the field's local units. Set to the card prefab's width.")]
        [Min(1f)] [SerializeField] private float _cardWidth = 150f;
        [Tooltip("Horizontal gap between cards, in the field's local units.")]
        [SerializeField] private float _spacing = 12f;

        [Header("Animation")]
        [SerializeField] private float _scaleUp = 1.15f;
        [SerializeField] private float _scaleDuration = 0.25f;
        [SerializeField] private float _panDuration = 1.2f;
        [SerializeField] private Ease _panEase = Ease.InOutSine;
        [Tooltip("Delay before the reveal starts, so it follows the panel's window pop-in.")]
        [SerializeField] private float _startDelay = 1.2f;
        [Tooltip("Max cells the player visibly climbs past, regardless of the true rank jump.")]
        [Min(1)] [SerializeField] private int _maxClimbCells = 3;

        private readonly List<TournamentRowView> _botCards = new List<TournamentRowView>();
        private readonly List<float> _botRestX = new List<float>();   // each bot's resting anchoredPosition.x
        private TournamentRowView _playerCard;
        private Sequence _sequence;

        private ITournamentService _service;          // cached while the strip is shown, for the countdown
        private float _tick;
        private int _lastShownMinutes = -1;
        private int _oldRank;                         // player's rank before this win (for the count-up)
        private int _newRank;                         // player's rank after this win

        private float Step => _cardWidth + _spacing;

        /// Builds the rail and runs the reveal (or shows it static). Called by the end screen,
        /// which supplies the rank change to animate (the widget keeps no game-side persistence).
        public void Play(RankReveal reveal = default)
        {
            var service = ServiceLocator.Instance?.TryResolve<ITournamentService>();
            if (service == null) { Hide(); return; }          // tournament locked / unavailable
            _service = service;

            var board = service.GetLeaderboard();
            int playerIndex = PlayerIndex(board);
            if (playerIndex < 0) { Hide(); return; }          // player not on the board

            int climb = 0;
            _oldRank = _newRank = 0;
            if (reveal.Improved)
            {
                _oldRank = reveal.OldRank;
                _newRank = reveal.NewRank;
                climb = Mathf.Clamp(reveal.OldRank - reveal.NewRank, 0, _maxClimbCells);
            }

            gameObject.SetActive(true);
            _lastShownMinutes = -1;                            // force a fresh format on the first frame
            UpdateCountdown();
            BuildField(board, playerIndex, climb);

            if (climb > 0)
            {
                _playerCard?.SetRank(_oldRank);               // start the player on its previous rank
                PlayClimb(climb);
            }
            else
            {
                Settle();                                     // no improvement: static, centred
            }
        }

        // Instantiates the player card into the centre pin and the bot cards into the field at their
        // resting slots (slot*step). Bots span the visible window plus the climb distance on the
        // better-ranked side so the viewport stays full for the whole slide.
        private void BuildField(IReadOnlyList<LeaderboardEntry> board, int playerIndex, int climb)
        {
            Clear();
            DisableDriver();

            // The authored field is a ScrollRect content (stretched anchors + layout group) and the
            // cards are bottom-left anchored for that layout. Normalise the field to a centred origin so
            // its slide and the cards' anchoredPosition maths are deterministic no matter how the prefab
            // was set up — the code no longer depends on prefab anchoring.
            NormalizeCentered(_field);
            _field.anchoredPosition = Vector2.zero;

            _playerCard = Instantiate(_cardPrefab, _playerPin);
            _playerCard.Bind(board[playerIndex], _catalog);
            var pinRT = (RectTransform)_playerCard.transform;
            NormalizeCentered(pinRT);
            // The pin lives under a different parent than the field, so don't trust its local origin —
            // pin the player card to the field's (resting) centre in world space. Both are under the same
            // panel, so this stays aligned through the window pop-in. The card never moves after this; it
            // only scales. The field slides beneath it.
            pinRT.position = _field.position;
            pinRT.localScale = Vector3.one;

            for (int d = -_visibleRadius; d <= _visibleRadius + climb; d++)
            {
                if (d == 0) continue;                         // the player's own slot; the pin covers it
                int idx = playerIndex + d;
                if (idx < 0 || idx >= board.Count) continue;  // off the board (e.g. near rank 1)
                var card = Instantiate(_cardPrefab, _field);
                card.Bind(board[idx], _catalog);
                var rt = (RectTransform)card.transform;
                NormalizeCentered(rt);
                rt.anchoredPosition = new Vector2(-d * Step, 0f);
                rt.localScale = Vector3.one;
                _botCards.Add(card);
                _botRestX.Add(-d * Step);
            }
        }

        // Refreshes the "time left until the weekly reset" label. Mirrors TournamentScreen: ticks on the
        // unscaled clock (the end screen runs at timeScale 0) and only re-formats when the minute changes
        // to avoid per-frame string allocations.
        private void Update()
        {
            if (_service == null || _countdownText == null) return;
            _tick += Time.unscaledDeltaTime;
            if (_tick < 1f) return;
            _tick = 0f;
            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (_service == null || _countdownText == null) return;
            var remaining = _service.WeekEndUtc - System.DateTime.UtcNow;
            if (remaining < System.TimeSpan.Zero) remaining = System.TimeSpan.Zero;

            int totalMinutes = (int)remaining.TotalMinutes;
            if (totalMinutes == _lastShownMinutes) return;     // avoid per-second string allocs
            _lastShownMinutes = totalMinutes;
            _countdownText.text = Format(remaining);
        }

        private static string Format(System.TimeSpan t)
        {
            if (t.Days > 0) return $"{t.Days}d {t.Hours}h";
            if (t.Hours > 0) return $"{t.Hours}h {t.Minutes}m";
            return $"{t.Minutes}m";
        }

        // Forces centred anchors + pivot so a RectTransform placed at anchoredPosition (x, 0) sits exactly
        // x from its parent's centre. The card prefab is authored bottom-left and the field is stretched
        // scroll content, so without this the slot maths is off and the slide doesn't translate cleanly.
        private static void NormalizeCentered(RectTransform rt)
        {
            if (rt == null) return;
            var centre = new Vector2(0.5f, 0.5f);
            rt.anchorMin = centre;
            rt.anchorMax = centre;
            rt.pivot = centre;
        }

        // 3-beat reveal: pop the player up, slide the bots left by climb*step (player visibly climbs
        // past the bots), pop the player in. The bots start shifted so the player's OLD-rank
        // neighbours flank the pin, and end at their rest slots (new-rank neighbours).
        private void PlayClimb(int climb)
        {
            var pinRT = (RectTransform)_playerCard.transform;

            // Beat 2 moves the bot cards themselves: each starts climb*Step to the RIGHT of its rest slot
            // (so the player's OLD-rank neighbours flank the pinned player) and slides to its rest slot,
            // making the player visibly climb past them. The player card stays put; only the bots move.
            _sequence?.Kill();
            _sequence = DOTween.Sequence();
            _sequence.SetUpdate(true);                        // end screen may run at timeScale 0
            _sequence.AppendInterval(_startDelay);
            _sequence.Append(pinRT.DOScale(_scaleUp, _scaleDuration));   // beat 1: pop up

            bool slideAppended = false;
            for (int i = 0; i < _botCards.Count; i++)
            {
                if (_botCards[i] == null) continue;
                var rt = (RectTransform)_botCards[i].transform;
                float restX = _botRestX[i];
                rt.anchoredPosition = new Vector2(restX + climb * Step, rt.anchoredPosition.y);
                var tw = rt.DOAnchorPosX(restX, _panDuration).SetEase(_panEase);
                if (!slideAppended) { _sequence.Append(tw); slideAppended = true; } // beat 2
                else _sequence.Join(tw);
            }

            // Beat 2 (parallel): count the player's rank number from its previous value up to the new
            // one, in lockstep with the slide. Ranking up means the number decreases (e.g. 50 -> 10).
            if (_oldRank != _newRank)
            {
                float counter = _oldRank;
                var rankTween = DOTween.To(() => counter, x =>
                    {
                        counter = x;
                        _playerCard?.SetRank(Mathf.RoundToInt(x));
                    }, _newRank, _panDuration).SetEase(_panEase);
                if (!slideAppended) { _sequence.Append(rankTween); slideAppended = true; }
                else _sequence.Join(rankTween);
            }

            _sequence.Append(pinRT.DOScale(1f, _scaleDuration));         // beat 3: pop in
            _sequence.OnComplete(Settle);
        }

        // Snaps to the canonical resting layout: field at 0, player centred at scale 1. Idempotent and
        // safe to call defensively, so an interrupted reveal can never leave a half-built strip.
        private void Settle()
        {
            if (_field != null) _field.anchoredPosition = Vector2.zero;
            for (int i = 0; i < _botCards.Count; i++)
            {
                if (_botCards[i] == null) continue;
                var rt = (RectTransform)_botCards[i].transform;
                rt.anchoredPosition = new Vector2(_botRestX[i], rt.anchoredPosition.y);
            }
            if (_playerCard != null)
            {
                var pinRT = (RectTransform)_playerCard.transform;
                pinRT.localScale = Vector3.one;
                if (_field != null) pinRT.position = _field.position;  // re-pin to the resting field centre
                if (_newRank > 0) _playerCard.SetRank(_newRank);       // ensure the count-up lands exactly
            }
        }

        // Belt-and-suspenders: if the field still carries layout/scroll components from the old prefab,
        // disable them so they can't fight the manual slot positions.
        private void DisableDriver()
        {
            if (_field == null) return;
            var hlg = _field.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.enabled = false;
            var fitter = _field.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            var scroll = _field.GetComponentInParent<ScrollRect>();
            if (scroll != null) scroll.enabled = false;
        }

        private static int PlayerIndex(IReadOnlyList<LeaderboardEntry> board)
        {
            if (board == null) return -1;
            for (int i = 0; i < board.Count; i++)
                if (board[i].IsPlayer) return i;
            return -1;
        }

        private void Hide()
        {
            _sequence?.Kill();
            _sequence = null;
            _service = null;                                   // stop the countdown ticking while hidden
            Clear();
            gameObject.SetActive(false);
        }

        private void Clear()
        {
            foreach (var c in _botCards)
                if (c != null) Destroy(c.gameObject);
            _botCards.Clear();
            _botRestX.Clear();
            if (_playerCard != null) { Destroy(_playerCard.gameObject); _playerCard = null; }
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }
    }
}
