using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sorolla.UI;

namespace Sorolla.Lives.UI
{
    /// <summary>
    /// Base "Out of Lives" panel. Sorolla Core ships behavior; _Game ships the prefab visuals
    /// and subscribes events to its currency / ad / booster wiring.
    /// Buttons assigned in the prefab stay visible; game code is responsible for hiding any
    /// that don't apply to its build.
    /// </summary>
    public class OutOfLivesPanel : UIPanel
    {
        [Header("Bindings (assign in prefab)")]
        [SerializeField] private TMP_Text _countdownLabel;
        [SerializeField] private TMP_Text _coinCostLabel;
        [SerializeField] private Button _watchAdButton;
        [SerializeField] private Button _spendCoinsButton;
        [SerializeField] private Button _buyBoosterButton;
        [SerializeField] private Button _closeButton;
        [Tooltip("Optional. Shown only while the WatchAd RV-for-life reward is on cooldown. Game-side wiring drives the visibility and text.")]
        [SerializeField] private TMP_Text _watchAdCooldownLabel;

        /// <summary>
        /// Set the displayed coin cost on the SpendCoins button label.
        /// Game-side wiring populates this with the value from Remote Config.
        /// </summary>
        public void SetCoinCost(int cost)
        {
            if (_coinCostLabel != null) _coinCostLabel.text = cost.ToString();
        }

        /// <summary>
        /// Toggle the WatchAd button's interactable state. Game-side wiring uses this to
        /// disable the button while the RV life-reward cooldown is active.
        /// </summary>
        public void SetWatchAdInteractable(bool interactable)
        {
            if (_watchAdButton != null) _watchAdButton.interactable = interactable;
        }

        /// <summary>
        /// Drive the optional WatchAd cooldown label. The label is shown only while
        /// <paramref name="remainingSeconds"/> &gt; 0; otherwise it is hidden.
        /// Format: HH:MM:SS for hour+ remainders, MM:SS otherwise.
        /// </summary>
        public void SetWatchAdCooldown(long remainingSeconds)
        {
            if (_watchAdCooldownLabel == null) return;
            if (remainingSeconds <= 0)
            {
                if (_watchAdCooldownLabel.gameObject.activeSelf)
                    _watchAdCooldownLabel.gameObject.SetActive(false);
                return;
            }
            if (!_watchAdCooldownLabel.gameObject.activeSelf)
                _watchAdCooldownLabel.gameObject.SetActive(true);
            var ts = TimeSpan.FromSeconds(remainingSeconds);
            _watchAdCooldownLabel.text = ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours:D1}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        public event Action OnWatchAdRequested;
        public event Action OnSpendCoinsRequested;
        public event Action OnBuyBoosterRequested;

        private ILivesService _lives;

        protected virtual void Awake()
        {
            if (_watchAdButton != null) _watchAdButton.onClick.AddListener(HandleWatchAdClick);
            if (_spendCoinsButton != null) _spendCoinsButton.onClick.AddListener(HandleSpendCoinsClick);
            if (_buyBoosterButton != null) _buyBoosterButton.onClick.AddListener(HandleBuyBoosterClick);
            if (_closeButton != null) _closeButton.onClick.AddListener(() => HideAsync().Forget());
        }

        private void HandleWatchAdClick()
        {
            if (OnWatchAdRequested == null)
                Debug.Log("[OutOfLivesPanel] WatchAd clicked — no game-side listener attached.");
            OnWatchAdRequested?.Invoke();
        }

        private void HandleSpendCoinsClick()
        {
            if (OnSpendCoinsRequested == null)
                Debug.Log("[OutOfLivesPanel] SpendCoins clicked — no game-side listener attached.");
            OnSpendCoinsRequested?.Invoke();
        }

        private void HandleBuyBoosterClick()
        {
            if (OnBuyBoosterRequested == null)
                Debug.Log("[OutOfLivesPanel] BuyBooster clicked — no game-side listener attached.");
            OnBuyBoosterRequested?.Invoke();
        }

        public override async UniTask ShowAsync(object args = null)
        {
            _lives = ServiceLocator.Instance.TryResolve<ILivesService>();
            await base.ShowAsync(args);
        }

        protected virtual void Update()
        {
            if (_lives == null || _countdownLabel == null) return;
            var t = _lives.TimeUntilNextLife;
            _countdownLabel.text = t > TimeSpan.Zero ? $"{t:mm\\:ss}" : string.Empty;
        }
    }
}
