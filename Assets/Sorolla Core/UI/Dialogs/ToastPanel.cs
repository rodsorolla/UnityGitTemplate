using System;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.UI.Dialogs
{
    /// <summary>
    /// Auto-dismissing toast notification panel.
    /// </summary>
    public class ToastPanel : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Transform _container;

        [Header("Timing")]
        [SerializeField] private float _defaultDisplayDuration = 2f;
        [SerializeField] private float _slideInDuration = 0.3f;
        [SerializeField] private float _slideOutDuration = 0.2f;

        [Header("Animation")]
        [SerializeField] private float _slideOffset = 200f;
        [SerializeField] private Ease _slideInEase = Ease.OutBack;
        [SerializeField] private Ease _slideOutEase = Ease.InBack;

        private Sequence _sequence;
        private Action _onDismissed;

        /// <summary>
        /// Data for configuring the toast.
        /// </summary>
        public class Data
        {
            public string Message;
            public Sprite Icon;
            public float? Duration;
            public Action OnDismissed;
        }

        protected virtual void Awake()
        {
            gameObject.SetActive(false);
        }

        protected virtual void OnDisable()
        {
            _sequence?.Kill();
        }

        public override async Task ShowAsync(object args = null)
        {
            if (args is Data data)
            {
                ConfigureToast(data);
                await PlayToastSequence(data.Duration ?? _defaultDisplayDuration);
            }
        }

        public override Task HideAsync()
        {
            _sequence?.Kill();
            gameObject.SetActive(false);
            RaiseClosed();
            return Task.CompletedTask;
        }

        public override bool HandleBack() => false; // Toasts don't block back

        private void ConfigureToast(Data data)
        {
            if (_messageText != null)
                _messageText.text = data.Message ?? "";

            if (_iconImage != null)
            {
                _iconImage.gameObject.SetActive(data.Icon != null);
                if (data.Icon != null)
                    _iconImage.sprite = data.Icon;
            }

            _onDismissed = data.OnDismissed;
        }

        private async Task PlayToastSequence(float displayDuration)
        {
            _sequence?.Kill();
            gameObject.SetActive(true);

            var rectTransform = _container as RectTransform;
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (rectTransform == null)
            {
                // Fallback: just wait and hide
                await Task.Delay((int)(displayDuration * 1000));
                await HideAsync();
                return;
            }

            var startPos = rectTransform.anchoredPosition;
            var hidePos = new Vector2(startPos.x, startPos.y - _slideOffset);

            // Start hidden
            rectTransform.anchoredPosition = hidePos;

            _sequence = DOTween.Sequence();

            // Slide in
            _sequence.Append(rectTransform.DOAnchorPos(startPos, _slideInDuration)
                .SetEase(_slideInEase));

            // Wait
            _sequence.AppendInterval(displayDuration);

            // Slide out
            _sequence.Append(rectTransform.DOAnchorPos(hidePos, _slideOutDuration)
                .SetEase(_slideOutEase));

            _sequence.SetUpdate(true);

            _sequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
                _onDismissed?.Invoke();
                RaiseClosed();
            });
            await _sequence.AsyncWaitForCompletion();
        }
    }
}
