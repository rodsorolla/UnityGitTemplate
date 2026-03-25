using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.UI
{
    /// <summary>
    /// Base settings panel with common options: close, haptics toggle, music/SFX volume.
    /// Games can extend this for additional settings.
    /// </summary>
    public class SettingsPanel : UIPanel
    {
        [Header("Close")]
        [SerializeField] private Button _closeButton;

        [Header("Haptics")]
        [SerializeField] private Toggle _hapticsToggle;

        [Header("Audio")]
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        private AudioManager _audioManager;
        private IHapticsService _hapticsService;
        private bool _pausedByPanel;

        protected virtual void Awake()
        {
            gameObject.SetActive(false);

            // Get services
            _audioManager = ServiceLocator.Instance.TryResolve<AudioManager>();
            _hapticsService = ServiceLocator.Instance.TryResolve<IHapticsService>();

            // Setup close button
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);

            // Setup haptics toggle
            if (_hapticsToggle != null)
            {
                _hapticsToggle.onValueChanged.AddListener(OnHapticsToggled);
            }

            // Setup audio toggles
            if (_musicToggle != null)
                _musicToggle.onValueChanged.AddListener(OnMusicToggled);

            if (_sfxToggle != null)
                _sfxToggle.onValueChanged.AddListener(OnSFXToggled);

            // Setup volume sliders
            if (_musicVolumeSlider != null)
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (_sfxVolumeSlider != null)
                _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        protected virtual void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);

            if (_hapticsToggle != null)
                _hapticsToggle.onValueChanged.RemoveListener(OnHapticsToggled);

            if (_musicToggle != null)
                _musicToggle.onValueChanged.RemoveListener(OnMusicToggled);

            if (_sfxToggle != null)
                _sfxToggle.onValueChanged.RemoveListener(OnSFXToggled);

            if (_musicVolumeSlider != null)
                _musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

            if (_sfxVolumeSlider != null)
                _sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }

        public override UniTask ShowAsync(object args = null)
        {
            // Pause game if not already paused
            if (!GameManager.IsPaused)
            {
                GameManager.Pause();
                _pausedByPanel = true;
            }

            RefreshUI();
            gameObject.SetActive(true);
            RaiseOpened();
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync()
        {
            // Resume only if we paused
            if (_pausedByPanel)
            {
                GameManager.Resume();
                _pausedByPanel = false;
            }

            gameObject.SetActive(false);
            RaiseClosed();
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Refresh UI to match current settings values.
        /// </summary>
        protected virtual void RefreshUI()
        {
            // Try to resolve services if not already resolved
            _audioManager ??= ServiceLocator.Instance.TryResolve<AudioManager>();
            _hapticsService ??= ServiceLocator.Instance.TryResolve<IHapticsService>();

            // Haptics - show/hide based on support, update toggle state
            if (_hapticsToggle != null)
            {
                bool supported = _hapticsService != null && _hapticsService.IsSupported;
                _hapticsToggle.gameObject.SetActive(supported);

                if (supported)
                {
                    _hapticsToggle.SetIsOnWithoutNotify(_hapticsService.IsEnabled);
                }
            }

            // Audio
            if (_audioManager != null)
            {
                if (_musicToggle != null)
                {
                    _musicToggle.SetIsOnWithoutNotify(_audioManager.MusicEnabled);
                }

                if (_sfxToggle != null)
                {
                    _sfxToggle.SetIsOnWithoutNotify(_audioManager.SFXEnabled);
                }

                if (_musicVolumeSlider != null)
                    _musicVolumeSlider.SetValueWithoutNotify(_audioManager.MusicVolume);

                if (_sfxVolumeSlider != null)
                    _sfxVolumeSlider.SetValueWithoutNotify(_audioManager.SFXVolume);
            }
        }

        private void OnCloseClicked()
        {
            _ = UIManager.Instance.ClosePanelAsync(this);
        }

        private void OnHapticsToggled(bool isOn)
        {
            if (_hapticsService != null)
            {
                _hapticsService.IsEnabled = isOn;

                // Play feedback when enabling
                if (isOn)
                {
                    _hapticsService.PlaySelection();
                }
            }
        }

        private void OnMusicToggled(bool isOn)
        {
            _audioManager?.SetMusicEnabled(isOn);
        }

        private void OnSFXToggled(bool isOn)
        {
            _audioManager?.SetSFXEnabled(isOn);
        }

        private void OnMusicVolumeChanged(float value)
        {
            _audioManager?.SetMusicVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            _audioManager?.SetSFXVolume(value);
        }
    }
}
