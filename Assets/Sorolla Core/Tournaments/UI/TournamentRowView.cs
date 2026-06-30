using System;
using Sorolla.Profile;
using Sorolla.Tournaments;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tournaments.UI
{
    /// One leaderboard row. Prefab authored by the user; fields assigned in the inspector.
    public class TournamentRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rank;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _trophies;
        [SerializeField] private Image _avatar;
        [SerializeField] private Image _flag;
        [SerializeField] private GameObject _playerHighlight;

        [Tooltip("Tap target on the chest. Only the 3 podium rows wire this; scroll rows leave it null.")]
        [SerializeField] private Button _chestButton;

        /// This row's leaderboard rank (1-based), cached from the last Bind().
        public int Rank { get; private set; }

        /// The chest's RectTransform — used to position the shared reward bubble over it.
        public RectTransform ChestAnchor =>
            _chestButton != null ? (RectTransform)_chestButton.transform : null;

        /// Raised when this row's chest is tapped. Null-safe: rows without a chest button never fire.
        public event Action<TournamentRowView> OnChestClicked;

        private void Awake()
        {
            if (_chestButton != null)
                _chestButton.onClick.AddListener(() => OnChestClicked?.Invoke(this));
        }

        /// Overrides just the displayed rank number (and cached Rank) without re-binding the row.
        /// Used by the rank-reveal strip to count the player's rank up during the climb animation.
        public void SetRank(int rank)
        {
            Rank = rank;
            if (_rank != null) _rank.text = rank.ToString();
        }

        public void Bind(LeaderboardEntry e, ProfileCatalog catalog)
        {
            Rank = e.Rank;
            if (_rank != null) _rank.text = e.Rank.ToString();
            if (_name != null) _name.text = e.DisplayName;
            if (_trophies != null) _trophies.text = e.Trophies.ToString();
            if (_avatar != null && catalog != null) _avatar.sprite = catalog.GetAvatarSprite(e.AvatarId);
            if (_flag != null && catalog != null) _flag.sprite = catalog.GetFlagSprite(e.CountryCode);
            if (_playerHighlight != null) _playerHighlight.SetActive(e.IsPlayer);
        }
    }
}
