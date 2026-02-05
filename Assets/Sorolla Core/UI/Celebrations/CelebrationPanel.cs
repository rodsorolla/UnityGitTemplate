using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.UI.Celebrations
{
    /// <summary>
    /// Base class for celebration/unlock panels.
    /// Implements Template Method pattern: ParseData -> UpdateUI -> PlayShowAnimation
    /// Subclasses implement ParseData and UpdateUI for their specific data types.
    /// </summary>
    /// <typeparam name="TData">The type of data this celebration displays</typeparam>
    public abstract class CelebrationPanel<TData> : UIPanel where TData : class
    {
        [Header("Common UI References")]
        [SerializeField] protected Button _confirmButton;
        [SerializeField] protected Transform _windowTransform;
        [SerializeField] protected ParticleSystem _celebrationFX;

        [Header("Animation Settings")]
        [SerializeField] protected float _showAnimationDuration = 0.5f;
        [SerializeField] protected Ease _showEase = Ease.OutBack;
        [SerializeField] protected float _hideAnimationDuration = 0.3f;
        [SerializeField] protected Ease _hideEase = Ease.InBack;

        [Header("Particles")]
        [SerializeField] protected float _particleDelay = 0f;

        protected TData _data;
        protected UIManager _uiManager;

        protected virtual void Awake()
        {
            gameObject.SetActive(false);

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        protected virtual void OnDestroy()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        public override async Task ShowAsync(object args = null)
        {
            _uiManager ??= UIManager.Instance;

            _data = ParseData(args);
            UpdateUI(_data);

            gameObject.SetActive(true);
            await PlayShowAnimation();
            RaiseOpened();
        }

        public override async Task HideAsync()
        {
            StopParticles();
            await PlayHideAnimation();
            gameObject.SetActive(false);
            RaiseClosed();
        }

        public override bool HandleBack()
        {
            HandleConfirmClicked();
            return true;
        }

        /// <summary>
        /// Parse the input args into strongly-typed data.
        /// Override for custom parsing logic.
        /// </summary>
        protected virtual TData ParseData(object args)
        {
            return args as TData;
        }

        /// <summary>
        /// Update UI elements based on the parsed data.
        /// Must be implemented by subclasses.
        /// </summary>
        protected abstract void UpdateUI(TData data);

        /// <summary>
        /// Play the show animation. Override for custom animations.
        /// </summary>
        protected virtual async Task PlayShowAnimation()
        {
            if (_windowTransform != null)
            {
                _windowTransform.localScale = Vector3.zero;
                await _windowTransform.DOScale(Vector3.one, _showAnimationDuration)
                    .SetEase(_showEase)
                    .SetUpdate(true)
                    .AsyncWaitForCompletion();
            }

            PlayParticles();
        }

        /// <summary>
        /// Play the hide animation. Override for custom animations.
        /// </summary>
        protected virtual async Task PlayHideAnimation()
        {
            if (_windowTransform != null)
            {
                await _windowTransform.DOScale(Vector3.zero, _hideAnimationDuration)
                    .SetEase(_hideEase)
                    .SetUpdate(true)
                    .AsyncWaitForCompletion();
            }
        }

        /// <summary>
        /// Play celebration particles with optional delay.
        /// </summary>
        protected virtual void PlayParticles()
        {
            if (_celebrationFX == null) return;

            if (_particleDelay > 0f)
            {
                DOVirtual.DelayedCall(_particleDelay, () =>
                {
                    if (_celebrationFX != null && gameObject.activeInHierarchy)
                        _celebrationFX.Play();
                }).SetUpdate(true);
            }
            else
            {
                _celebrationFX.Play();
            }
        }

        /// <summary>
        /// Stop celebration particles.
        /// </summary>
        protected virtual void StopParticles()
        {
            if (_celebrationFX != null)
                _celebrationFX.Stop();
        }

        protected virtual async void HandleConfirmClicked()
        {
            if (_uiManager != null)
            {
                await _uiManager.ClosePanelAsync(this);
            }
            else
            {
                await HideAsync();
            }
        }
    }
}
