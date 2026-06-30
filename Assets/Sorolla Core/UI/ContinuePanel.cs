using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.UI
{
    /// <summary>
    /// Continue panel shown after game over. Offers paid continue option if eligible.
    /// </summary>
    public class ContinuePanel : UIPanel
    {
        [Header("Restart Button")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private TextMeshProUGUI _continueButtonText;

        [Header("Pay to Continue")]
        [SerializeField] private GameObject _payToContinueContainer;
        [SerializeField] private Button _payToContinueButton;
        [SerializeField] private TextMeshProUGUI _priceText;

        [Header("Watch Ad to Continue")]
        [SerializeField] private GameObject _watchAdContainer;
        [SerializeField] private Button _watchAdButton;

        /// <summary>
        /// Fired when player clicks the pay-to-continue button.
        /// </summary>
        public event Action OnPayToContinueClicked;

        /// <summary>
        /// Fired when player clicks the watch-ad-to-continue button.
        /// Game preserves this flow alongside the upstream pay-to-continue option.
        /// </summary>
        public event Action OnWatchAdClicked;

        // Live-affordability probe captured at Show. Evaluated again at click time so
        // a top-up via NotEnoughCoinsPanel takes effect immediately. Falls back to the
        // snapshot bool when the caller didn't supply a probe.
        private Func<bool> _canAffordProbe;
        private bool _canAffordSnapshot;

        private IHapticsService _haptics;
        private IHapticsService Haptics => _haptics ??= ServiceLocator.Instance?.TryResolve<IHapticsService>();

        /// <summary>
        /// Data passed to configure the panel.
        /// </summary>
        public class Data
        {
            public bool CanPayToContinue;
            public int Price;
            public bool CanAfford;
            public bool CanWatchAd;

            /// <summary>
            /// Optional live affordability probe — re-evaluated at click time so
            /// purchasing more coins from NotEnoughCoinsPanel unblocks the button
            /// without rebuilding the panel.
            /// </summary>
            public Func<bool> CanAffordLive;
        }

        private void Awake()
        {
            gameObject.SetActive(false);

            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnRestartClicked);

            if (_payToContinueButton != null)
                _payToContinueButton.onClick.AddListener(OnPayToContinueButtonClicked);

            if (_watchAdButton != null)
                _watchAdButton.onClick.AddListener(OnWatchAdButtonClicked);
        }

        private void OnDestroy()
        {
            if (_continueButton != null)
                _continueButton.onClick.RemoveListener(OnRestartClicked);

            if (_payToContinueButton != null)
                _payToContinueButton.onClick.RemoveListener(OnPayToContinueButtonClicked);

            if (_watchAdButton != null)
                _watchAdButton.onClick.RemoveListener(OnWatchAdButtonClicked);
        }

        public override UniTask ShowAsync(object args = null)
        {
            var data = args as Data;
            ConfigurePayToContinue(data);
            ConfigureWatchAd(data);
            gameObject.SetActive(true);
            RaiseOpened();
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync()
        {
            gameObject.SetActive(false);
            RaiseClosed();
            return UniTask.CompletedTask;
        }

        private void ConfigurePayToContinue(Data data)
        {
            if (_payToContinueContainer == null) return;

            
            bool showPayOption = data?.CanPayToContinue == true;
            _payToContinueContainer.SetActive(showPayOption);
            
            _continueButtonText.text = showPayOption ? "Never mind..." : "Continue";

            if (!showPayOption) return;

            if (_priceText != null)
                _priceText.text = data.Price.ToString();

            // Keep the button interactable regardless of affordability — when the player
            // can't afford, clicking opens NotEnoughCoinsPanel rather than no-op.
            _canAffordSnapshot = data.CanAfford;
            _canAffordProbe = data.CanAffordLive;
            if (_payToContinueButton != null)
                _payToContinueButton.interactable = true;
        }

        private void OnRestartClicked()
        {
            _ = HideAsync();
        }

        private void OnPayToContinueButtonClicked()
        {
            Haptics?.PlayImpact(HapticsIntensity.Light);

            bool canAffordNow = _canAffordProbe != null ? _canAffordProbe.Invoke() : _canAffordSnapshot;
            if (!canAffordNow)
            {
                UIManager.Instance?.OpenPanelAsync(UIPanelId.NotEnoughCoins).Forget();
                return;
            }
            OnPayToContinueClicked?.Invoke();
            _ = HideAsync();
        }

        private void ConfigureWatchAd(Data data)
        {
            if (_watchAdContainer == null) return;
            bool show = data?.CanWatchAd == true;
            _watchAdContainer.SetActive(show);
        }

        private void OnWatchAdButtonClicked()
        {
            Haptics?.PlayImpact(HapticsIntensity.Light);
            OnWatchAdClicked?.Invoke();
        }
    }
}
