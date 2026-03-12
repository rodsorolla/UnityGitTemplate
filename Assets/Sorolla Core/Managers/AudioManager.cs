using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Sorolla
{
    public class AudioManager : SorollaManager
    {
        public enum Channel { Master, Music, SFX, UI }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer mixer;
        
        [Header("Exposed Parameter Names")]
        [Tooltip("Must match the exposed parameter names in your AudioMixer")]
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";
        [SerializeField] private string uiVolumeParam = "UIVolume";

        [Header("Mixer Groups (Optional)")]
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup uiGroup;

        [Header("Audio Library")]
        [SerializeField] private AudioLibrary audioLibrary;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Settings")]
        [SerializeField] private bool autoSave = true;
        [SerializeField] private string saveKey = "sorolla_audio";

        // Volume state (0-1)
        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private float _uiVolume = 1f;

        // Enabled state
        private bool _masterEnabled = true;
        private bool _musicEnabled = true;
        private bool _sfxEnabled = true;
        private bool _uiEnabled = true;

        private const float MuteDb = -80f;

        // Pending music track (for when music is disabled at start)
        private string _pendingMusicKey;

        // Public read-only access to current values
        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;
        public float UIVolume => _uiVolume;
        public bool MasterEnabled => _masterEnabled;
        public bool MusicEnabled => _musicEnabled;
        public bool SFXEnabled => _sfxEnabled;
        public bool UIEnabled => _uiEnabled;

        protected override void Initialize()
        {
            // Initialize audio library
            audioLibrary?.Initialize();

            // Load saved settings
            LoadSettings();

            // Initialize audio sources if not assigned
            SetupAudioSources();

            // Note: ApplyAll is deferred to Start() because AudioMixer.SetFloat
            // doesn't work reliably during Awake/initialization phase
        }

        private void Start()
        {
            // Ensure saved settings are loaded before applying to mixer
            // (Init is idempotent — safe even if GameManager already called it)
            Init();

            // Apply all volume settings to mixer after AudioMixer is fully initialized
            ApplyAll();
        }

        private void SetupAudioSources()
        {
            if (musicSource == null)
            {
                var musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                var sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (uiSource == null)
            {
                var uiObj = new GameObject("UISource");
                uiObj.transform.SetParent(transform);
                uiSource = uiObj.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
            }

            // Assign mixer groups
            if (musicGroup != null) musicSource.outputAudioMixerGroup = musicGroup;
            if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;
            if (uiGroup != null) uiSource.outputAudioMixerGroup = uiGroup;
        }

        #region Mixer Volume Control

        public void SetVolume(Channel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);
            switch (channel)
            {
                case Channel.Master: _masterVolume = volume; break;
                case Channel.Music: _musicVolume = volume; break;
                case Channel.SFX: _sfxVolume = volume; break;
                case Channel.UI: _uiVolume = volume; break;
            }
            ApplyChannel(channel);
            if (autoSave) SaveSettings();
        }

        public void SetEnabled(Channel channel, bool enabled)
        {
            switch (channel)
            {
                case Channel.Master: _masterEnabled = enabled; break;
                case Channel.Music: _musicEnabled = enabled; break;
                case Channel.SFX: _sfxEnabled = enabled; break;
                case Channel.UI: _uiEnabled = enabled; break;
            }
            ApplyChannel(channel);
            if (autoSave) SaveSettings();
        }

        public float GetVolume(Channel channel)
        {
            return channel switch
            {
                Channel.Master => _masterVolume,
                Channel.Music => _musicVolume,
                Channel.SFX => _sfxVolume,
                Channel.UI => _uiVolume,
                _ => 1f
            };
        }

        public bool GetEnabled(Channel channel)
        {
            return channel switch
            {
                Channel.Master => _masterEnabled,
                Channel.Music => _musicEnabled,
                Channel.SFX => _sfxEnabled,
                Channel.UI => _uiEnabled,
                _ => true
            };
        }

        private void ApplyAll()
        {
            ApplyChannel(Channel.Master);
            ApplyChannel(Channel.Music);
            ApplyChannel(Channel.SFX);
            ApplyChannel(Channel.UI);
        }

        private void ApplyChannel(Channel channel)
        {
            if (mixer == null) return;

            string param = GetParamName(channel);
            if (string.IsNullOrEmpty(param)) return;

            float volume = GetVolume(channel);
            bool enabled = GetEnabled(channel);
            float db = enabled ? LinearToDb(volume) : MuteDb;

            mixer.SetFloat(param, db);
        }

        private string GetParamName(Channel channel)
        {
            return channel switch
            {
                Channel.Master => masterVolumeParam,
                Channel.Music => musicVolumeParam,
                Channel.SFX => sfxVolumeParam,
                Channel.UI => uiVolumeParam,
                _ => null
            };
        }

        private static float LinearToDb(float linear)
        {
            return Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
        }

        #endregion

        #region Save/Load

        private void SaveSettings()
        {
            var data = new AudioSaveData
            {
                masterVolume = _masterVolume,
                musicVolume = _musicVolume,
                sfxVolume = _sfxVolume,
                uiVolume = _uiVolume,
                masterEnabled = _masterEnabled,
                musicEnabled = _musicEnabled,
                sfxEnabled = _sfxEnabled,
                uiEnabled = _uiEnabled
            };
            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            if (!PlayerPrefs.HasKey(saveKey)) return;

            try
            {
                var json = PlayerPrefs.GetString(saveKey);
                var data = JsonUtility.FromJson<AudioSaveData>(json);
                if (data != null)
                {
                    _masterVolume = Mathf.Clamp01(data.masterVolume);
                    _musicVolume = Mathf.Clamp01(data.musicVolume);
                    _sfxVolume = Mathf.Clamp01(data.sfxVolume);
                    _uiVolume = Mathf.Clamp01(data.uiVolume);
                    _masterEnabled = data.masterEnabled;
                    _musicEnabled = data.musicEnabled;
                    _sfxEnabled = data.sfxEnabled;
                    _uiEnabled = data.uiEnabled;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AudioManager] Failed to load settings: {e.Message}");
            }
        }

        [Serializable]
        private class AudioSaveData
        {
            public float masterVolume = 1f;
            public float musicVolume = 1f;
            public float sfxVolume = 1f;
            public float uiVolume = 1f;
            public bool masterEnabled = true;
            public bool musicEnabled = true;
            public bool sfxEnabled = true;
            public bool uiEnabled = true;
        }

        #endregion

        #region Music

        public void PlayMusic(string key, bool loop = true)
        {
            _pendingMusicKey = key;

            if (!_musicEnabled) return;

            var entry = audioLibrary?.GetMusic(key);
            if (entry?.clip != null)
                PlayMusic(entry.clip, loop, entry.volume);
            else
                Debug.LogWarning($"[AudioManager] Music key not found: {key}");
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float volume = 1f)
        {
            if (clip == null) return;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = volume;
            musicSource.Play();
        }
        
        public void PlayMusicRandom(string[] keys, bool loop = true)
        {
            if (keys == null || keys.Length == 0) return;
            var randomKey = keys[UnityEngine.Random.Range(0, keys.Length)];
            PlayMusic(randomKey, loop);
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        /// <summary>
        /// Stops the music with a fade out effect.
        /// </summary>
        /// <param name="fadeOutDuration">Duration of the fade out in seconds.</param>
        public void StopMusic(float fadeOutDuration)
        {
            if (fadeOutDuration <= 0f)
            {
                StopMusic();
                return;
            }

            StartCoroutine(FadeOutMusicCoroutine(fadeOutDuration));
        }

        private IEnumerator FadeOutMusicCoroutine(float duration)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = startVolume; // Restore original volume for next playback
        }

        public void PauseMusic()
        {
            musicSource.Pause();
        }

        public void ResumeMusic()
        {
            musicSource.UnPause();
        }

        public bool IsMusicPlaying => musicSource.isPlaying;

        #endregion

        #region SFX

        public void PlaySFX(string key)
        {
            if (!_sfxEnabled) return;

            var entry = audioLibrary?.GetSFX(key);
            if (entry?.clip != null)
                PlaySFX(entry.clip, entry.volume);
            else
                Debug.LogWarning($"[AudioManager] SFX key not found: {key}");
        }

        public void PlaySFX(AudioClip clip)
        {
            if (!_sfxEnabled) return;
            if (clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void PlaySFXRandom(string[] keys)
        {
            if (!_sfxEnabled) return;
            if (keys == null || keys.Length == 0) return;
            var randomKey = keys[UnityEngine.Random.Range(0, keys.Length)];
            PlaySFX(randomKey);
        }

        public void PlaySFX(AudioClip clip, float volumeScale)
        {
            if (!_sfxEnabled) return;
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, volumeScale);
        }

        public void PlaySFXAtPosition(AudioClip clip, Vector3 position)
        {
            if (!_sfxEnabled) return;
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position);
        }

        public void PlaySFXAtPosition(string key, Vector3 position)
        {
            if (!_sfxEnabled) return;

            var entry = audioLibrary?.GetSFX(key);
            if (entry?.clip != null)
                AudioSource.PlayClipAtPoint(entry.clip, position, entry.volume);
            else
                Debug.LogWarning($"[AudioManager] SFX key not found: {key}");
        }

        #endregion

        #region UI Sounds

        public void PlayUISound(string key)
        {
            if (!_uiEnabled) return;

            var entry = audioLibrary?.GetUI(key);
            if (entry?.clip != null)
                uiSource.PlayOneShot(entry.clip, entry.volume);
            else
                Debug.LogWarning($"[AudioManager] UI sound key not found: {key}");
        }

        public void PlayUISound(AudioClip clip)
        {
            if (!_uiEnabled) return;
            if (clip == null) return;
            uiSource.PlayOneShot(clip);
        }

        #endregion

        #region Convenience Methods (for UI sliders/toggles)

        public void SetMasterVolume(float volume) => SetVolume(Channel.Master, volume);
        public void SetMusicVolume(float volume) => SetVolume(Channel.Music, volume);
        public void SetSFXVolume(float volume) => SetVolume(Channel.SFX, volume);
        public void SetUIVolume(float volume) => SetVolume(Channel.UI, volume);

        public void SetMasterEnabled(bool enabled) => SetEnabled(Channel.Master, enabled);
        public void SetMusicEnabled(bool enabled)
        {
            SetEnabled(Channel.Music, enabled);

            if (!enabled)
            {
                // Stop playback when music is disabled
                musicSource.Stop();
            }
            else if (!string.IsNullOrEmpty(_pendingMusicKey) && !musicSource.isPlaying)
            {
                // Start pending music when re-enabled
                PlayMusic(_pendingMusicKey);
            }
        }
        public void SetSFXEnabled(bool enabled) => SetEnabled(Channel.SFX, enabled);
        public void SetUIEnabled(bool enabled) => SetEnabled(Channel.UI, enabled);

        /// <summary>
        /// Reset all audio settings to defaults and clear saved data.
        /// </summary>
        public void ResetToDefaults()
        {
            _masterVolume = 1f;
            _musicVolume = 1f;
            _sfxVolume = 1f;
            _uiVolume = 1f;
            _masterEnabled = true;
            _musicEnabled = true;
            _sfxEnabled = true;
            _uiEnabled = true;
            
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.Save();
            
            ApplyAll();
            Debug.Log("[AudioManager] Reset to defaults.");
        }

        /// <summary>
        /// Log current audio state for debugging.
        /// </summary>
        [ContextMenu("Debug Audio State")]
        public void DebugAudioState()
        {
            Debug.Log($"[AudioManager] Master: {_masterVolume:F2} (enabled: {_masterEnabled})");
            Debug.Log($"[AudioManager] Music: {_musicVolume:F2} (enabled: {_musicEnabled})");
            Debug.Log($"[AudioManager] SFX: {_sfxVolume:F2} (enabled: {_sfxEnabled})");
            Debug.Log($"[AudioManager] UI: {_uiVolume:F2} (enabled: {_uiEnabled})");
            Debug.Log($"[AudioManager] SFX Source: {(sfxSource != null ? $"volume={sfxSource.volume}, mute={sfxSource.mute}" : "NULL")}");
            Debug.Log($"[AudioManager] Mixer: {(mixer != null ? mixer.name : "NULL")}");
        }

        #endregion
    }
}
