using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Highlight adapter for UI targets (RectTransform + Canvas + GraphicRaycaster).
    /// Elevates the target by toggling <see cref="Canvas.overrideSorting"/>,
    /// swapping the sorting layer to <c>TutorialHighlight</c> and raising
    /// <see cref="Canvas.sortingOrder"/>. The layer swap matters: Unity sorts by
    /// sortingLayer first and order second, so raising order alone is not enough
    /// when the target Canvas sits on a layer below the tutorial overlay.
    /// </summary>
    public sealed class UIHighlightAdapter : ITutorialHighlightable
    {
        private const string HighlightSortingLayer = "TutorialHighlight";

        private readonly RectTransform _rect;
        private readonly Canvas _canvas;
        private readonly GraphicRaycaster _raycaster;
        private readonly string _id;

        // Original sorting state, recorded on Elevate so Restore is exact.
        private bool _wasElevated;
        private bool _origOverrideSorting;
        private int _origSortingOrder;
        private int _origSortingLayerId;

        public UIHighlightAdapter(string id, RectTransform rect, Canvas canvas, GraphicRaycaster raycaster)
        {
            _id = id;
            _rect = rect;
            _canvas = canvas;
            _raycaster = raycaster;
        }

        public string Id => _id;
        public GameObject GameObject => _rect != null ? _rect.gameObject : null;

        public Vector2 GetScreenCenter(Camera worldCamera)
        {
            if (_rect == null) return Vector2.zero;
            Vector3 worldCenter = _rect.TransformPoint(_rect.rect.center);
            return ToScreen(worldCenter);
        }

        public Rect GetScreenBounds(Camera worldCamera)
        {
            if (_rect == null) return Rect.zero;

            var corners = new Vector3[4];
            _rect.GetWorldCorners(corners);

            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < 4; i++)
            {
                Vector2 screen = ToScreen(corners[i]);
                if (screen.x < min.x) min.x = screen.x;
                if (screen.y < min.y) min.y = screen.y;
                if (screen.x > max.x) max.x = screen.x;
                if (screen.y > max.y) max.y = screen.y;
            }

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        /// <summary>
        /// The owning Canvas determines how a UI world-point maps to screen pixels:
        /// Overlay canvases store rect world-positions IN screen pixels already (no
        /// camera projection), Camera/World canvases use the canvas's worldCamera.
        /// </summary>
        private Vector2 ToScreen(Vector3 uiWorld)
        {
            var root = _canvas != null ? _canvas.rootCanvas : null;
            if (root == null) return uiWorld;

            if (root.renderMode == RenderMode.ScreenSpaceOverlay)
                return uiWorld;

            var cam = root.worldCamera != null ? root.worldCamera : Camera.main;
            return cam != null ? (Vector2)cam.WorldToScreenPoint(uiWorld) : (Vector2)uiWorld;
        }

        public void Elevate(int highlightLayer)
        {
            if (_canvas == null || _wasElevated) return;

            if (SortingLayer.NameToID(HighlightSortingLayer) == 0 && !SortingLayerExists(HighlightSortingLayer))
            {
                Debug.LogError(
                    "[UIHighlightAdapter] Sorting layer 'TutorialHighlight' is missing. " +
                    "Run Sorolla/Tutorial/Setup Highlight System.");
                return;
            }

            _origOverrideSorting = _canvas.overrideSorting;
            _origSortingOrder = _canvas.sortingOrder;
            _origSortingLayerId = _canvas.sortingLayerID;
            _canvas.overrideSorting = true;
            _canvas.sortingLayerName = HighlightSortingLayer;
            _canvas.sortingOrder = highlightLayer;
            _wasElevated = true;
        }

        public void Restore()
        {
            if (!_wasElevated) return;
            if (_canvas != null)
            {
                _canvas.overrideSorting = _origOverrideSorting;
                _canvas.sortingLayerID = _origSortingLayerId;
                _canvas.sortingOrder = _origSortingOrder;
            }
            _wasElevated = false;
        }

        private static bool SortingLayerExists(string name)
        {
            var layers = SortingLayer.layers;
            for (int i = 0; i < layers.Length; i++)
                if (layers[i].name == name) return true;
            return false;
        }
    }
}
