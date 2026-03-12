using UnityEngine;

namespace Sorolla.UI
{
    /// <summary>
    /// Adjusts a RectTransform's anchors to fit within the device safe area.
    /// Attach to any UI element that should respect notches/status bars.
    /// Per-edge toggles allow applying only to specific sides.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        [Header("Edges to Apply")]
        [SerializeField] private bool _applyTop = true;
        [SerializeField] private bool _applyBottom = true;
        [SerializeField] private bool _applyLeft = true;
        [SerializeField] private bool _applyRight = true;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private ScreenOrientation _lastOrientation;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea || _lastOrientation != Screen.orientation)
                ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            var safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;
            _lastOrientation = Screen.orientation;

            var screenWidth = Screen.width;
            var screenHeight = Screen.height;

            if (screenWidth <= 0 || screenHeight <= 0) return;

            var anchorMin = new Vector2(
                _applyLeft ? safeArea.x / screenWidth : 0f,
                _applyBottom ? safeArea.y / screenHeight : 0f
            );

            var anchorMax = new Vector2(
                _applyRight ? (safeArea.x + safeArea.width) / screenWidth : 1f,
                _applyTop ? (safeArea.y + safeArea.height) / screenHeight : 1f
            );

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}
