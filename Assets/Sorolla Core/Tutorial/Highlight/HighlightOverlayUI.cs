using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Screen-space UI overlay for tutorial highlighting.
    /// Uses ScreenSpaceCamera mode so it renders as part of the main camera output,
    /// allowing the highlight overlay camera to render highlighted items on top.
    /// Tutorial panels (ScreenSpaceOverlay) render above everything.
    /// Rendering order: Scene → Dark overlay → Highlighted items → Tutorial panel.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class HighlightOverlayUI : HighlightOverlayBase
    {
        [Header("Settings")]
        [SerializeField] private Color _overlayColor = new Color(0f, 0f, 0f, 0.7f);
        [SerializeField] private Camera _camera;

        private Canvas _canvas;
        private Image _overlayImage;

        private void Awake()
        {
            CreateOverlay();
            Hide();
        }

        private void CreateOverlay()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _camera != null ? _camera : Camera.main;
            _canvas.planeDistance = 1f;
            _canvas.sortingOrder = 100;

            // Create full-screen image
            var imageGO = new GameObject("OverlayImage");
            imageGO.transform.SetParent(transform, false);

            _overlayImage = imageGO.AddComponent<Image>();
            _overlayImage.color = _overlayColor;
            _overlayImage.raycastTarget = false;

            // Stretch to fill
            var rect = imageGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public override void Show()
        {
            _overlayImage.gameObject.SetActive(true);
        }

        public override void Hide()
        {
            _overlayImage.gameObject.SetActive(false);
        }
    }
}
