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

        /// <summary>
        /// Fired when player clicks the pay-to-continue button.
        /// </summary>
        public event Action OnPayToContinueClicked;

        /// <summary>
        /// Data passed to configure the panel.
        /// </summary>
        public class Data
        {
            public bool CanPayToContinue;
            public int Price;
            public bool CanAfford;
        }

        private void Awake()
        {
            gameObject.SetActive(false);

            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnRestartClicked);

            if (_payToContinueButton != null)
                _payToContinueButton.onClick.AddListener(OnPayToContinueButtonClicked);
        }

        private void OnDestroy()
        {
            if (_continueButton != null)
                _continueButton.onClick.RemoveListener(OnRestartClicked);

            if (_payToContinueButton != null)
                _payToContinueButton.onClick.RemoveListener(OnPayToContinueButtonClicked);
        }

        public override UniTask ShowAsync(object args = null)
        {
            ConfigurePayToContinue(args as Data);
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

            if (_payToContinueButton != null)
                _payToContinueButton.interactable = data.CanAfford;
        }

        private void OnRestartClicked()
        {
            _ = HideAsync();
        }

        private void OnPayToContinueButtonClicked()
        {
            OnPayToContinueClicked?.Invoke();
            _ = HideAsync();
        }
    }
}
