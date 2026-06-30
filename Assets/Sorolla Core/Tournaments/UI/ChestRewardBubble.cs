using System.Collections.Generic;
using Sorolla.UI;
using Sorolla.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tournaments.UI
{
    /// Shared reward "bulle" popup for the tournament leaderboard. A single instance lives under
    /// TournamentScreen and is repositioned over whichever podium chest was tapped. One RewardBulle
    /// is spawned per reward into the container; a full-screen blocker behind it closes the popup
    /// when the player taps anywhere else. Prefab authored by the user (ChestRewardBulle).
    public class ChestRewardBubble : MonoBehaviour
    {
        [Tooltip("The bubble body that is shown/hidden and repositioned over the chest.")]
        [SerializeField] private RectTransform _root;

        [Tooltip("Full-screen transparent button behind the bubble — closes on tap-elsewhere.")]
        [SerializeField] private Button _blocker;

        [Tooltip("RewardBulle prefab spawned once per reward.")]
        [SerializeField] private RewardBulleView _rewardBullePrefab;

        [Tooltip("Parent the spawned RewardBulle entries live under (usually a layout group).")]
        [SerializeField] private RectTransform _slotsContainer;

        [Tooltip("World-space offset applied to the bubble relative to the tapped chest.")]
        [SerializeField] private Vector2 _worldOffset = Vector2.zero;

        private readonly List<RewardBulleView> _spawned = new List<RewardBulleView>();

        private void Awake()
        {
            if (_blocker != null)
                _blocker.onClick.AddListener(Hide);
            Hide();
        }

        /// Positions the bubble over <paramref name="anchor"/>, spawns one entry per reward and shows it.
        public void Show(RectTransform anchor, EventReward[] rewards, IIconResolver icons)
        {
            if (_root == null || anchor == null) return;

            // Activate BEFORE positioning: writing a world position to a RectTransform whose
            // hierarchy was inactive uses a stale parent matrix on the first show, landing the
            // bubble off the chest. Activating first keeps the very first tap consistent.
            if (_blocker != null) _blocker.gameObject.SetActive(true);
            _root.gameObject.SetActive(true);

            SpawnRewards(rewards, icons);
            _root.position = anchor.position + (Vector3)_worldOffset;
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
            if (_blocker != null) _blocker.gameObject.SetActive(false);
        }

        private void SpawnRewards(EventReward[] rewards, IIconResolver icons)
        {
            ClearSpawned();
            if (rewards == null || _rewardBullePrefab == null || _slotsContainer == null) return;

            foreach (var reward in rewards)
            {
                if (reward == null) continue;
                var entry = Instantiate(_rewardBullePrefab, _slotsContainer);
                entry.Bind(reward, icons);
                _spawned.Add(entry);
            }
        }

        private void ClearSpawned()
        {
            foreach (var entry in _spawned)
                if (entry != null) Destroy(entry.gameObject);
            _spawned.Clear();
        }
    }
}
