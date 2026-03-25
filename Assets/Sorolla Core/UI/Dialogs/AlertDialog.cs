using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.UI.Dialogs
{
    /// <summary>
    /// Simple alert dialog with a single OK button.
    /// </summary>
    public class AlertDialog : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _okButton;
        [SerializeField] private TextMeshProUGUI _okButtonText;

        [Header("Animation")]
        [SerializeField] private Transform _windowTransform;
        [SerializeField] private float _animDuration = 0.3f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;

        private Action _onDismissed;
        private UniTaskCompletionSource<bool> _tcs;

        /// <summary>
        /// Data for configuring the alert dialog.
        /// </summary>
        public class Data
        {
            public string Title;
            public string Message;
            public string OkText = "OK";
            public Action OnDismissed;
        }

        protected virtual void Awake()
        {
            gameObject.SetActive(false);

            if (_okButton != null)
                _okButton.onClick.AddListener(OnOkClicked);
        }

        protected virtual void OnDestroy()
        {
            if (_okButton != null)
                _okButton.onClick.RemoveListener(OnOkClicked);
        }

        public override async UniTask ShowAsync(object args = null)
        {
            if (args is Data data)
            {
                ConfigureDialog(data);
            }

            gameObject.SetActive(true);
            await PlayEnterAnimation();
            RaiseOpened();
        }

        public override async UniTask HideAsync()
        {
            await PlayExitAnimation();
            gameObject.SetActive(false);
            RaiseClosed();
        }

        public override bool HandleBack()
        {
            OnOkClicked();
            return true;
        }

        public override int BackPriority => 100;

        /// <summary>
        /// Show the dialog and wait for dismissal.
        /// </summary>
        public async UniTask ShowAndWaitAsync(Data data)
        {
            _tcs = new UniTaskCompletionSource<bool>();
            await ShowAsync(data);
            await _tcs.Task;
        }

        private void ConfigureDialog(Data data)
        {
            if (_titleText != null)
                _titleText.text = data.Title ?? "";

            if (_messageText != null)
                _messageText.text = data.Message ?? "";

            if (_okButtonText != null)
                _okButtonText.text = data.OkText ?? "OK";

            _onDismissed = data.OnDismissed;
        }

        private async UniTask PlayEnterAnimation()
        {
            if (_windowTransform != null)
            {
                _windowTransform.localScale = Vector3.zero;
                await _windowTransform.DOScale(1f, _animDuration)
                    .SetEase(_showEase)
                    .SetUpdate(true)
                    .AsyncWaitForCompletion();
            }
        }

        private async UniTask PlayExitAnimation()
        {
            if (_windowTransform != null)
            {
                await _windowTransform.DOScale(0f, _animDuration)
                    .SetEase(_hideEase)
                    .SetUpdate(true)
                    .AsyncWaitForCompletion();
            }
        }

        private async void OnOkClicked()
        {
            _onDismissed?.Invoke();
            _tcs?.TrySetResult(true);

            var uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                await uiManager.ClosePanelAsync(this);
            }
            else
            {
                await HideAsync();
            }
        }
    }
}
