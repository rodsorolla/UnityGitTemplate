using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sorolla;
using Sorolla.Events;
using Sorolla.Tournaments;
using Sorolla.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tournaments.UI
{
    /// Top-3 chest reward panel. Chest visual is chosen by podium rank
    /// (1 = gold, 2 = silver, 3 = bronze). The open button plays the chest-open animation
    /// (on a separate GameObject) and its onClick calls RevealRewards() to show the reward
    /// contents. Claim grants and closes. Prefab authored by the user.
    ///
    /// Reward-grant invariant: this panel is opened ONLY for ranks 1-3 by
    /// TournamentResultsPanel, and its Claim is the single call to
    /// ClaimPendingResultAsync() for those ranks (grants + clears pending).
    public class TournamentRewardPanel : UIPanel
    {
        /// One chest-state Image plus its per-tier sprite set
        /// (index by podium rank: 0 = gold/1st, 1 = silver/2nd, 2 = bronze/3rd).
        [Serializable]
        public class TieredChestImage
        {
            public Image image;
            public Sprite[] tierSprites = new Sprite[3];
        }

        [Header("Chest images (sprite chosen per tier: 0 = gold/1st, 1 = silver/2nd, 2 = bronze/3rd)")]
        [SerializeField] private TieredChestImage _closedChest;
        [SerializeField] private TieredChestImage _openChest;
        [SerializeField] private TieredChestImage _openLidChest;

        [Header("Reward reveal")]
        [SerializeField] private Button _openButton;          // tap target; fires the open anim + reveals rewards
        [SerializeField] private GameObject _rewardsRoot;     // bulles + claim; hidden until RevealRewards()

        [Header("Reward bulles (one spawned per reward, placed at the matching spawn point)")]
        [SerializeField] private RewardBulleView _rewardBullePrefab;
        [SerializeField] private Transform[] _spawnPoints = new Transform[5];

        [Header("Claim")]
        [SerializeField] private Button _claimButton;

        [Header("Reveal animation")]
        [Tooltip("Delay after tapping Open before rewards pop in — lets the chest open animation play first.")]
        [SerializeField] private float _revealDelay = 0.5f;
        [SerializeField] private float _popDuration = 0.35f;
        [Tooltip("Extra delay between each bulle so they pop one after another.")]
        [SerializeField] private float _popStagger = 0.08f;
        [SerializeField] private Ease _popEase = Ease.OutBack;
        [Tooltip("Final scale of each bulle, relative to its prefab size (2 = double).")]
        [SerializeField] private float _finalScale = 2f;

        private ITournamentService _service;
        private IIconResolver _icons;
        private bool _opened;
        private readonly List<RewardBulleView> _spawnedRewards = new List<RewardBulleView>();
        private readonly List<Vector3> _spawnedScales = new List<Vector3>();

        public override async UniTask ShowAsync(object args = null)
        {
            _service = ServiceLocator.Instance.TryResolve<ITournamentService>();
            _icons = ServiceLocator.Instance.TryResolve<IIconResolver>();
            _opened = false;
            Bind();
            await base.ShowAsync(args);
        }

        private void Bind()
        {
            var pr = _service?.GetPendingResult();
            int rank = pr != null ? pr.FinalRank : 1;

            // Chest sprite by podium rank (1..3 -> 0..2). Assign the tier sprite on each
            // chest-state image; which image is shown stays prefab-authored (open animation).
            int tierIndex = Mathf.Clamp(rank - 1, 0, 2);
            ApplyTierSprite(_closedChest, tierIndex);
            ApplyTierSprite(_openChest, tierIndex);
            ApplyTierSprite(_openLidChest, tierIndex);

            // Resolve and spawn rewards for display only (no grant yet).
            SpawnRewards(ResolveRewards(pr));

            // Closed state: rewards (bulles + claim live under _rewardsRoot) hidden until
            // the open button reveals them via RevealRewards().
            if (_rewardsRoot != null) _rewardsRoot.SetActive(false);

            if (_openButton != null)
            {
                _openButton.onClick.RemoveListener(RevealRewards);
                _openButton.onClick.AddListener(RevealRewards);
            }
            if (_claimButton != null)
            {
                _claimButton.interactable = true;   // re-enable on each open (panel is pooled)
                _claimButton.onClick.RemoveListener(OnClaim);
                _claimButton.onClick.AddListener(OnClaim);
            }
        }

        private static void ApplyTierSprite(TieredChestImage chest, int tierIndex)
        {
            if (chest == null || chest.image == null || chest.tierSprites == null) return;
            if (tierIndex < 0 || tierIndex >= chest.tierSprites.Length) return;
            chest.image.sprite = chest.tierSprites[tierIndex];
        }

        private EventReward[] ResolveRewards(PendingResult pr)
        {
            if (pr == null || _service == null) return Array.Empty<EventReward>();
            var tiers = _service.Tiers;
            if (tiers == null || pr.TierIndex < 0 || pr.TierIndex >= tiers.Count)
                return Array.Empty<EventReward>();
            return tiers[pr.TierIndex].PodiumReward(pr.FinalRank) ?? Array.Empty<EventReward>();
        }

        /// Spawns one RewardBulle per reward at the matching spawn point and binds it.
        /// Capped at the number of spawn points provided (up to 5).
        private void SpawnRewards(EventReward[] rewards)
        {
            ClearSpawnedRewards();
            if (rewards == null || _rewardBullePrefab == null || _spawnPoints == null) return;

            int count = Mathf.Min(rewards.Length, _spawnPoints.Length);
            for (int i = 0; i < count; i++)
            {
                var reward = rewards[i];
                var point = _spawnPoints[i];
                if (reward == null || point == null) continue;

                // worldPositionStays:false keeps the prefab's local transform, placing it on the spawn point.
                var bulle = Instantiate(_rewardBullePrefab, point, false);
                bulle.Bind(reward, _icons);

                // Remember the authored scale, then collapse to 0 so it can pop in on reveal.
                _spawnedScales.Add(bulle.transform.localScale);
                bulle.transform.localScale = Vector3.zero;

                _spawnedRewards.Add(bulle);
            }
        }

        private void ClearSpawnedRewards()
        {
            foreach (var bulle in _spawnedRewards)
                if (bulle != null) Destroy(bulle.gameObject);
            _spawnedRewards.Clear();
            _spawnedScales.Clear();
        }

        /// Reveals the reward contents. Wired to the open button's onClick (the open
        /// animation lives on a separate GameObject, so an Animation Event can't reach here).
        /// Waits for the chest open animation, then pops the bulles in one after another.
        public void RevealRewards()
        {
            if (_opened) return;
            _opened = true;
            RevealRewardsAsync().Forget();
        }

        private async UniTaskVoid RevealRewardsAsync()
        {
            // Let the chest open animation play before the rewards spring out of it.
            if (_revealDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(_revealDelay), ignoreTimeScale: true);

            if (_rewardsRoot != null) _rewardsRoot.SetActive(true);

            // Pop each bulle from 0 to its authored scale, staggered so they cascade out.
            for (int i = 0; i < _spawnedRewards.Count; i++)
            {
                var bulle = _spawnedRewards[i];
                if (bulle == null) continue;

                bulle.transform.localScale = Vector3.zero;
                bulle.transform.DOScale(_spawnedScales[i] * _finalScale, _popDuration)
                    .SetEase(_popEase)
                    .SetDelay(_popStagger * i)
                    .SetUpdate(true)
                    .SetLink(bulle.gameObject);
            }
        }

        private void OnClaim()
        {
            if (_service == null) { HideAsync().Forget(); return; }
            if (_claimButton != null) _claimButton.interactable = false;   // block double-claim taps
            ClaimAndCloseAsync().Forget();
        }

        private async UniTaskVoid ClaimAndCloseAsync()
        {
            await _service.ClaimPendingResultAsync();
            await HideAsync();
        }
    }
}
