using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Sorolla.UI.Effects
{
    /// <summary>
    /// Standalone effect: when the attached TMP text content changes, plays a
    /// punch-scale animation and/or a color flash. Event-driven via
    /// <see cref="TMPro_EventManager.TEXT_CHANGED_EVENT"/> — no per-frame polling.
    /// Animates <c>transform.localScale</c> and TMP vertex color only, so TMP
    /// batching is preserved (no per-instance material is created).
    /// Works with both TextMeshPro (world-space) and TextMeshProUGUI.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public class TextChangeEffect : MonoBehaviour
    {
        [Header("Scale Punch")]
        [SerializeField] private bool _playScale = true;
        [SerializeField] private float _punchScale = 1.2f;
        [SerializeField] private float _scaleInDuration = 0.12f;
        [SerializeField] private float _scaleOutDuration = 0.18f;
        [SerializeField] private Ease _scaleInEase = Ease.OutBack;
        [SerializeField] private Ease _scaleOutEase = Ease.OutCubic;

        [Header("Color Flash (optional)")]
        [SerializeField] private bool _playColor = false;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _colorFlashDuration = 0.25f;

        [Header("Particles (optional)")]
        [Tooltip("Particle system to restart on every trigger. Leave empty to skip.")]
        [SerializeField] private ParticleSystem _particles;

        [Header("Trigger")]
        [Tooltip("Minimum seconds between retriggers. Debounces rapid text updates (timers, counters).")]
        [Min(0f)] [SerializeField] private float _minInterval = 0.05f;

        [Tooltip("Skip the first text change after enable — useful so initial text assignment doesn't animate.")]
        [SerializeField] private bool _skipFirstChange = true;

        [Tooltip("Use unscaled time (ignores Time.timeScale / pause).")]
        [SerializeField] private bool _useUnscaledTime = true;

        private TMP_Text _text;
        private Sequence _sequence;
        private Vector3 _baseScale;
        private Color _baseColor;
        private string _lastText;
        private float _lastPlayTime = -999f;
        private bool _awaitingFirst;

        private float Now => _useUnscaledTime ? Time.unscaledTime : Time.time;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _baseScale = transform.localScale;
            _baseColor = _text.color;
            _lastText = _text.text;
        }

        private void OnEnable()
        {
            _awaitingFirst = _skipFirstChange;
            if (_text != null)
            {
                _lastText = _text.text;
                _baseColor = _text.color;
            }
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTmpTextChanged);
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTmpTextChanged);
            _sequence?.Kill();
            transform.localScale = _baseScale;
            if (_playColor && _text != null) _text.color = _baseColor;
        }

        private void OnTmpTextChanged(UnityEngine.Object obj)
        {
            if (!ReferenceEquals(obj, _text)) return;

            string current = _text.text;
            if (current == _lastText) return;
            _lastText = current;

            if (_awaitingFirst)
            {
                _awaitingFirst = false;
                return;
            }

            if (Now - _lastPlayTime < _minInterval) return;

            PlayNow();
        }

        /// <summary>
        /// Sets the text and forces the effect to play (bypasses debounce and first-change skip).
        /// </summary>
        public void SetText(string value)
        {
            if (_text == null) return;
            _text.text = value;
            _lastText = value;
            _awaitingFirst = false;
            PlayNow();
        }

        /// <summary>
        /// Manually trigger the effect regardless of text state.
        /// </summary>
        public void PlayNow()
        {
            if (_text == null) return;

            _lastPlayTime = Now;
            _sequence?.Kill();

            FireParticles();

            _sequence = DOTween.Sequence().SetUpdate(_useUnscaledTime);

            if (_playScale)
            {
                transform.localScale = _baseScale;
                _sequence.Append(transform.DOScale(_baseScale * _punchScale, _scaleInDuration).SetEase(_scaleInEase));
                _sequence.Append(transform.DOScale(_baseScale, _scaleOutDuration).SetEase(_scaleOutEase));
            }

            if (_playColor)
            {
                float half = _colorFlashDuration * 0.5f;
                _text.color = _baseColor;
                _sequence.Insert(0f, _text.DOColor(_flashColor, half));
                _sequence.Insert(half, _text.DOColor(_baseColor, half));
            }
        }

        private void FireParticles()
        {
            if (_particles == null) return;
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particles.Play(true);
        }

        /// <summary>
        /// Build a preview sequence using the current inspector settings.
        /// Used by the editor-only custom inspector to drive DOTweenEditorPreview.
        /// </summary>
        public Sequence BuildPreviewSequence(out Vector3 baseScale, out Color baseColor)
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            baseScale = transform.localScale;
            baseColor = _text != null ? _text.color : Color.white;

            FireParticles();

            var seq = DOTween.Sequence();

            if (_playScale)
            {
                seq.Append(transform.DOScale(baseScale * _punchScale, _scaleInDuration).SetEase(_scaleInEase));
                seq.Append(transform.DOScale(baseScale, _scaleOutDuration).SetEase(_scaleOutEase));
            }

            if (_playColor && _text != null)
            {
                float half = _colorFlashDuration * 0.5f;
                seq.Insert(0f, _text.DOColor(_flashColor, half));
                seq.Insert(half, _text.DOColor(baseColor, half));
            }

            return seq;
        }

        public void RestorePreviewState(Vector3 baseScale, Color baseColor)
        {
            transform.localScale = baseScale;
            if (_playColor && _text != null) _text.color = baseColor;
        }
    }
}
