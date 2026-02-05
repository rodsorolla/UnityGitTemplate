using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Sorolla.UI.Effects
{
    /// <summary>
    /// Billboard floating text with DOTween animation.
    /// Used by FloatingTextManager for pooled text effects.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class FloatingTextPopup : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float _duration = 0.8f;
        [SerializeField] private float _floatHeight = 1.5f;
        [SerializeField] private Ease _moveEase = Ease.OutCubic;
        [SerializeField] private Ease _scaleEase = Ease.OutBack;
        [SerializeField] private Ease _fadeEase = Ease.InQuad;

        [Header("Scale")]
        [SerializeField] private float _startScale = 0.5f;
        [SerializeField] private float _peakScale = 1.2f;
        [SerializeField] private float _endScale = 0.8f;
        [SerializeField] private float _scalePeakTime = 0.3f;

        [Header("Fade")]
        [SerializeField] private float _fadeStartTime = 0.5f;

        private TextMeshPro _text;
        private Camera _camera;
        private Sequence _sequence;
        private Vector3 _baseScale;

        private void Awake()
        {
            _text = GetComponent<TextMeshPro>();
            _baseScale = transform.localScale;
        }

        private void LateUpdate()
        {
            // Billboard: face camera
            if (_camera != null)
            {
                transform.rotation = _camera.transform.rotation;
            }
        }

        private void OnDisable()
        {
            _sequence?.Kill();
        }

        private void DisableSelf() => gameObject.SetActive(false);

        /// <summary>
        /// Play the floating text animation.
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="worldPosition">Starting world position</param>
        /// <param name="color">Text color</param>
        /// <param name="camera">Camera to billboard towards</param>
        /// <param name="scale">Scale multiplier</param>
        public void Play(string text, Vector3 worldPosition, Color color, Camera camera, float scale = 1f)
        {
            _camera = camera;

            // Setup initial state
            transform.position = worldPosition;
            transform.localScale = _baseScale * _startScale * scale;

            _text.text = text;
            _text.color = color;

            gameObject.SetActive(true);

            // Kill any existing animation
            _sequence?.Kill();

            // Create animation sequence
            _sequence = DOTween.Sequence();

            // Float upward
            _sequence.Append(transform.DOMoveY(worldPosition.y + _floatHeight * scale, _duration)
                .SetEase(_moveEase));

            // Scale animation: pop in, then shrink slightly
            _sequence.Insert(0f, transform.DOScale(_baseScale * _peakScale * scale, _scalePeakTime)
                .SetEase(_scaleEase));
            _sequence.Insert(_scalePeakTime, transform.DOScale(_baseScale * _endScale * scale, _duration - _scalePeakTime)
                .SetEase(Ease.Linear));

            // Fade out
            _sequence.Insert(_fadeStartTime, _text.DOFade(0f, _duration - _fadeStartTime)
                .SetEase(_fadeEase));

            _sequence.OnComplete(DisableSelf);

            _sequence.Play();
        }

        /// <summary>
        /// Stop the animation and return to pool.
        /// </summary>
        public void Stop()
        {
            _sequence?.Kill();
        }
    }
}
