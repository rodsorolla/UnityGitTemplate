using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Sorolla
{
    /// <summary>
    /// Displays a hand/finger sprite that follows the mouse cursor.
    /// Used for recording App Store videos with Unity Recorder.
    /// The sprite pivot should be at the tip of the index finger.
    /// </summary>
    public class FakeTouchCursor : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("References")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _cursorImage;

        [Header("Tap Animation")]
        [SerializeField] private float _tapScale = 0.85f;
        [SerializeField] private float _tapSpeed = 12f;

        [Header("Tap FX (Optional)")]
        [SerializeField] private ParticleSystem _tapFX;

        private RectTransform _cursorRect;
        private Vector3 _defaultScale;
        private Camera _canvasCamera;
        private Mouse _mouse;

        private void Awake()
        {
            _cursorRect = _cursorImage.rectTransform;
            _defaultScale = _cursorRect.localScale;
            _canvasCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            _mouse = Mouse.current;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (_mouse == null) return;

            var mousePos = _mouse.position.ReadValue();

            // Follow mouse position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                mousePos,
                _canvasCamera,
                out var localPoint);

            _cursorRect.localPosition = localPoint;

            // Tap animation: scale down on press, spring back on release
            var pressed = _mouse.leftButton.isPressed;
            var target = pressed ? _defaultScale * _tapScale : _defaultScale;
            _cursorRect.localScale = Vector3.Lerp(_cursorRect.localScale, target, Time.unscaledDeltaTime * _tapSpeed);

            // Spawn particle FX on click at the cursor pivot position
            if (_mouse.leftButton.wasPressedThisFrame && _tapFX != null)
            {
                _tapFX.transform.position = _cursorRect.position;
                _tapFX.Play();
            }
        }

        private void OnDestroy()
        {
            Cursor.visible = true;
        }
#endif
    }
}
