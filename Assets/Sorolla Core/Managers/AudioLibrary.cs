using UnityEngine;
using System.Collections.Generic;

namespace Sorolla
{
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Sorolla/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [System.Serializable]
        public class SFXEntry
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.5f, 1.5f)] public float pitchVariation = 1f;
        }

        [System.Serializable]
        public class MusicEntry
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        [Header("Sound Effects")]
        public List<SFXEntry> sfx = new();

        [Header("Music Tracks")]
        public List<MusicEntry> music = new();

        [Header("UI Sounds")]
        public List<SFXEntry> ui = new();

        // Runtime lookup dictionaries
        private Dictionary<string, SFXEntry> _sfxLookup;
        private Dictionary<string, MusicEntry> _musicLookup;
        private Dictionary<string, SFXEntry> _uiLookup;
        private bool _initialized;

        public void Initialize()
        {
            if (_initialized) return;

            _sfxLookup = new Dictionary<string, SFXEntry>();
            foreach (var entry in sfx)
            {
                if (entry.clip != null)
                    _sfxLookup[entry.clip.name] = entry;
            }

            _musicLookup = new Dictionary<string, MusicEntry>();
            foreach (var entry in music)
            {
                if (entry.clip != null)
                    _musicLookup[entry.clip.name] = entry;
            }

            _uiLookup = new Dictionary<string, SFXEntry>();
            foreach (var entry in ui)
            {
                if (entry.clip != null)
                    _uiLookup[entry.clip.name] = entry;
            }

            _initialized = true;
        }

        public SFXEntry GetSFX(string key)
        {
            if (!_initialized) Initialize();
            return _sfxLookup.TryGetValue(key, out var entry) ? entry : null;
        }

        public MusicEntry GetMusic(string key)
        {
            if (!_initialized) Initialize();
            return _musicLookup.TryGetValue(key, out var entry) ? entry : null;
        }

        public SFXEntry GetUI(string key)
        {
            if (!_initialized) Initialize();
            return _uiLookup.TryGetValue(key, out var entry) ? entry : null;
        }

        /// <summary>
        /// Reset initialization state (useful for hot-reload in editor)
        /// </summary>
        public void Reset()
        {
            _initialized = false;
            _sfxLookup = null;
            _musicLookup = null;
            _uiLookup = null;
        }

        private void OnEnable()
        {
            // Reset on domain reload to rebuild dictionaries
            _initialized = false;
        }
    }
}
