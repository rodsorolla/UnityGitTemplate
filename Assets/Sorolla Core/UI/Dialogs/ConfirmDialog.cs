using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.UI.Dialogs
{
    /// <summary>
    /// Generic confirmation dialog with configurable buttons.
    /// </summary>
    public class ConfirmDialog : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _confirmButtonText;
        [SerializeField] private TextMeshProUGUI _cancelButtonText;

        [Header("Animation")]
        [SerializeField] private Transform _windowTransform;
        [SerializeField] private float _animDuration = 0.3f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;

        private Action<bool> _resultCallback;
        private UniTaskCompletionSource<bool> _tcs;

        /// <summary>
        /// Data for configuring the confirm dialog.
        /// </summary>
        public class Data
        {
            public string Title;
            public string Message;
            public string ConfirmText = "OK";
            public string CancelText = "Cancel";
            public Action<bool> OnResult;
            public bool ShowCancelButton = true;
        }

        protected virtual void Awake()
        {
            gameObject.SetActive(false);

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(OnConfirmClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancelClicked);
        }

        protected virtual void OnDestroy()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(OnConfirmClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(OnCancelClicked);
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
            OnCancelClicked();
            return true;
        }

        public override int BackPriority => 100; // High priority to intercept back

        /// <summary>
        /// Show the dialog and wait for a result.
        /// </summary>
        /// <returns>True if confirmed, false if cancelled</returns>
        public async UniTask<bool> ShowAndWaitAsync(Data data)
        {
            _tcs = new UniTaskCompletionSource<bool>();
            await ShowAsync(data);
            return await _tcs.Task;
        }

        private void ConfigureDialog(Data data)
        {
            if (_titleText != null)
                _titleText.text = data.Title ?? "";

            if (_messageText != null)
                _messageText.text = data.Message ?? "";

            if (_confirmButtonText != null)
                _confirmButtonText.text = data.ConfirmText ?? "OK";

            if (_cancelButtonText != null)
                _cancelButtonText.text = data.CancelText ?? "Cancel";

            if (_cancelButton != null)
                _cancelButton.gameObject.SetActive(data.ShowCancelButton);

            _resultCallback = data.OnResult;
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

        private void OnConfirmClicked()
        {
            Complete(true);
        }

        private void OnCancelClicked()
        {
            Complete(false);
        }

        private async void Complete(bool result)
        {
            _resultCallback?.Invoke(result);
            _tcs?.TrySetResult(result);

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
