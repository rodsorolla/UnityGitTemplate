using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sorolla;
using Sorolla.Profile;
using Sorolla.Tournaments;
using Sorolla.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tournaments.UI
{
    /// End-of-week results + claim. Player-centric summary: final rank, adaptive outcome,
    /// tier transition. Hands off to TournamentRewardPanel for podium claims. Prefab authored by the user.
    public class TournamentResultsPanel : UIPanel
    {
        [SerializeField] private TMP_Text _rank;
        [SerializeField] private TMP_Text _outcome;

        [Header("Player profile")]
        [SerializeField] private Image _profileAvatar;
        [SerializeField] private Image _profileFlag;
        [SerializeField] private TMP_Text _profileName;
        [SerializeField] private ProfileCatalog _catalog;
        // Acts as "Continue": top-3 -> chest reward panel, rank 4+ -> claim & close.
        [SerializeField] private Button _claimButton;
        [Tooltip("Label on _claimButton. 'Claim Reward' for podium, 'Continue' otherwise.")]
        [SerializeField] private TMP_Text _claimLabel;

        [Header("Outcome badge")]
        [Tooltip("Graphics (Images and/or TMP texts) tinted by the outcome accent color.")]
        [SerializeField] private Graphic[] _accentGraphics;
        [SerializeField] private Color _promotedColor = new Color(0.30f, 0.78f, 0.33f); // green
        [SerializeField] private Color _stayedColor   = new Color(0.26f, 0.56f, 0.93f); // blue
        [SerializeField] private Color _demotedColor  = new Color(0.95f, 0.61f, 0.07f); // amber
        [Tooltip("Optional direction glyph image. Leave unassigned to skip.")]
        [SerializeField] private Image _outcomeGlyph;
        [SerializeField] private Sprite _glyphUp;
        [SerializeField] private Sprite _glyphFlat;
        [SerializeField] private Sprite _glyphDown;

        [Header("Tier transition")]
        [Tooltip("Tier cup sprites in tier order (mirror TournamentScreen._tierCupSprites).")]
        [SerializeField] private Sprite[] _tierCupSprites;
        [Tooltip("Left side (old tier + arrow). Hidden for the 'Stayed' outcome.")]
        [SerializeField] private GameObject _oldTierGroup;
        [SerializeField] private Image _oldTierIcon;
        [SerializeField] private TMP_Text _oldTierName;
        [SerializeField] private GameObject _tierArrow;
        [Tooltip("Right side: the current tier after the week resolved.")]
        [SerializeField] private Image _newTierIcon;
        [SerializeField] private TMP_Text _newTierName;

        private ITournamentService _service;
        private IPlayerProfile _profile;
        private bool _continuing;
        private UIPanel _chestPanel;

        public override async UniTask ShowAsync(object args = null)
        {
            _continuing = false;
            // Clear any dangling chest subscription left by a prior aborted flow (panel is pooled/reused).
            if (_chestPanel != null) { _chestPanel.OnClosed -= OnChestClosed; _chestPanel = null; }
            _service = ServiceLocator.Instance.TryResolve<ITournamentService>();
            _profile = ServiceLocator.Instance.TryResolve<IPlayerProfile>();
            Bind();
            await base.ShowAsync(args);
        }

        private void Bind()
        {
            ApplyProfile();
            var pr = _service?.GetPendingResult();
            if (pr != null)
            {
                if (_rank != null) _rank.text = "#" + pr.FinalRank;
                if (_outcome != null) _outcome.text = OutcomeText(pr.Outcome);
                ApplyOutcomeVisuals(pr.Outcome);
                ApplyTierTransition(pr);
                if (_claimLabel != null)
                {
                    bool podium = pr.FinalRank >= 1 && pr.FinalRank <= 3;
                    _claimLabel.text = podium ? "Claim Reward" : "Continue";
                }
            }
            if (_claimButton != null)
            {
                _claimButton.onClick.RemoveListener(OnClaim);
                _claimButton.onClick.AddListener(OnClaim);
            }
        }

        private void OnClaim()
        {
            if (_service == null || _continuing) return;
            _continuing = true;
            ContinueAsync().Forget();
        }

        // Rank 1-3: hand off to the chest reward panel (which claims). Rank 4+: claim
        // directly (grants nothing, clears pending) and close. Exactly one claim per week.
        private async UniTaskVoid ContinueAsync()
        {
            var pr = _service.GetPendingResult();
            bool podium = pr != null && pr.FinalRank >= 1 && pr.FinalRank <= 3;

            if (podium)
            {
                var ui = UIManager.Instance;
                if (ui != null)
                {
                    var chest = await ui.OpenPanelAsync(UIPanelId.TournamentReward);
                    if (chest != null)
                    {
                        _chestPanel = chest;
                        chest.OnClosed += OnChestClosed;
                        return; // stay open behind the chest; close when it closes
                    }
                }
                // Fallback if the chest panel can't open: claim here so rewards aren't lost.
                await _service.ClaimPendingResultAsync();
                await HideAsync();
                return;
            }

            await _service.ClaimPendingResultAsync();
            await HideAsync();
        }

        private void OnChestClosed(UIPanel chest)
        {
            chest.OnClosed -= OnChestClosed;
            _chestPanel = null;
            HideAsync().Forget();
        }

        private static string OutcomeText(TournamentOutcome o)
        {
            switch (o)
            {
                case TournamentOutcome.Promoted: return "You've got promoted!";
                case TournamentOutcome.Demoted: return "You've got demoted! - Climb back";
                default: return "You held your league";
            }
        }

        private Color OutcomeColor(TournamentOutcome o)
        {
            switch (o)
            {
                case TournamentOutcome.Promoted: return _promotedColor;
                case TournamentOutcome.Demoted: return _demotedColor;
                default: return _stayedColor;
            }
        }

        private Sprite OutcomeGlyph(TournamentOutcome o)
        {
            switch (o)
            {
                case TournamentOutcome.Promoted: return _glyphUp;
                case TournamentOutcome.Demoted: return _glyphDown;
                default: return _glyphFlat;
            }
        }

        // Player avatar, flag, and name resolved via ProfileCatalog (mirrors TournamentScreen).
        private void ApplyProfile()
        {
            if (_profileName != null) _profileName.text = _profile?.DisplayName ?? string.Empty;
            if (_profileAvatar != null && _catalog != null)
            {
                var sprite = _catalog.GetAvatarSprite(_profile?.AvatarId);
                if (sprite == null) sprite = _catalog.GetAvatarSprite(_catalog.FirstAvatarId);
                _profileAvatar.enabled = sprite != null;
                if (sprite != null) _profileAvatar.sprite = sprite;
            }
            if (_profileFlag != null && _catalog != null)
            {
                var flag = _catalog.GetFlagSprite(_profile?.CountryCode);
                _profileFlag.enabled = flag != null;
                if (flag != null) _profileFlag.sprite = flag;
            }
        }

        private void ApplyOutcomeVisuals(TournamentOutcome o)
        {
            var color = OutcomeColor(o);
            if (_accentGraphics != null)
            {
                for (int i = 0; i < _accentGraphics.Length; i++)
                    if (_accentGraphics[i] != null) _accentGraphics[i].color = color;
            }
            if (_outcomeGlyph != null)
            {
                var g = OutcomeGlyph(o);
                _outcomeGlyph.enabled = g != null;
                if (g != null) _outcomeGlyph.sprite = g;
            }
        }

        // Old tier = pr.TierIndex (captured before the transition); new tier = service.CurrentTierIndex
        // (already advanced). Stayed: show only the current-tier badge, no arrow/old badge.
        private void ApplyTierTransition(PendingResult pr)
        {
            var tiers = _service?.Tiers;
            int newIdx = _service?.CurrentTierIndex ?? 0;
            int oldIdx = pr != null ? pr.TierIndex : newIdx;
            bool moved = pr != null && pr.Outcome != TournamentOutcome.Stayed;

            SetTierBadge(_newTierIcon, _newTierName, tiers, newIdx);

            if (_oldTierGroup != null) _oldTierGroup.SetActive(moved);
            if (_tierArrow != null) _tierArrow.SetActive(moved);
            if (moved) SetTierBadge(_oldTierIcon, _oldTierName, tiers, oldIdx);
        }

        private void SetTierBadge(Image icon, TMP_Text label, IReadOnlyList<TierDefinition> tiers, int idx)
        {
            bool valid = tiers != null && idx >= 0 && idx < tiers.Count;
            if (label != null) label.text = valid ? tiers[idx].name : string.Empty;
            if (icon != null)
            {
                bool hasSprite = _tierCupSprites != null && idx >= 0 && idx < _tierCupSprites.Length;
                icon.enabled = hasSprite;
                if (hasSprite) icon.sprite = _tierCupSprites[idx];
            }
        }
    }
}
