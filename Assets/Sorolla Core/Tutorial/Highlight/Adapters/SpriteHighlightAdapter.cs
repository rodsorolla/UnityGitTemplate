using UnityEngine;
using UnityEngine.Rendering;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Highlight adapter for world-space targets rendered via
    /// <see cref="SortingGroup"/> or a bare <see cref="SpriteRenderer"/>.
    ///
    /// IMPORTANT: This adapter only swaps the sorting **layer** — not the
    /// <c>sortingOrder</c>. Views like <c>DefenderView</c> write
    /// <c>sortingOrder</c> every <c>LateUpdate</c> based on Y position; if we
    /// touched it the LateUpdate would fight us and the elevation would flicker.
    /// </summary>
    public sealed class SpriteHighlightAdapter : ITutorialHighlightable
    {
        private const string HighlightSortingLayer = "TutorialHighlight";

        private readonly string _id;
        private readonly Transform _transform;
        private readonly SortingGroup _sortingGroup;  // may be null
        private readonly SpriteRenderer _spriteRenderer; // may be null

        private bool _wasElevated;
        private int _origSortingLayerId;

        public SpriteHighlightAdapter(string id, SortingGroup sortingGroup)
        {
            _id = id;
            _sortingGroup = sortingGroup;
            _transform = sortingGroup != null ? sortingGroup.transform : null;
        }

        public SpriteHighlightAdapter(string id, SpriteRenderer spriteRenderer)
        {
            _id = id;
            _spriteRenderer = spriteRenderer;
            _transform = spriteRenderer != null ? spriteRenderer.transform : null;
        }

        public string Id => _id;
        public GameObject GameObject => _transform != null ? _transform.gameObject : null;

        public Vector2 GetScreenCenter(Camera worldCamera)
        {
            var cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return Vector2.zero;

            // SortingGroup case (composite sprite — defenders, enemies): prefer the
            // root transform as the logical anchor. Encapsulating every child
            // Renderer drifts the center when shadows/outlines extend past the body.
            // Single SpriteRenderer case: bounds.center is fine.
            Vector3 world =
                _sortingGroup != null && _transform != null ? _transform.position :
                _spriteRenderer != null ? _spriteRenderer.bounds.center :
                _transform != null ? _transform.position :
                Vector3.zero;
            return cam.WorldToScreenPoint(world);
        }

        public Rect GetScreenBounds(Camera worldCamera)
        {
            Camera projection = worldCamera != null ? worldCamera : Camera.main;
            if (projection == null) return Rect.zero;

            Bounds b;
            if (_spriteRenderer != null)
            {
                b = _spriteRenderer.bounds;
            }
            else if (_sortingGroup != null)
            {
                b = ComputeSortingGroupBounds(_sortingGroup);
            }
            else if (_transform != null)
            {
                b = new Bounds(_transform.position, Vector3.zero);
            }
            else
            {
                return Rect.zero;
            }

            // Project the 8 corners of the world-space AABB to screen and take the
            // enclosing rect. Good enough for ring sizing / pointer placement.
            Vector3 min = b.min, max = b.max;
            Vector2 sMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 sMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                Vector2 s = projection.WorldToScreenPoint(corner);
                if (s.x < sMin.x) sMin.x = s.x;
                if (s.y < sMin.y) sMin.y = s.y;
                if (s.x > sMax.x) sMax.x = s.x;
                if (s.y > sMax.y) sMax.y = s.y;
            }
            return new Rect(sMin.x, sMin.y, sMax.x - sMin.x, sMax.y - sMin.y);
        }

        public void Elevate(int highlightLayer)
        {
            if (_wasElevated) return;
            int highlightLayerId = SortingLayer.NameToID(HighlightSortingLayer);
            if (highlightLayerId == 0 && !SortingLayerExists(HighlightSortingLayer))
            {
                Debug.LogError(
                    "[SpriteHighlightAdapter] Sorting layer 'TutorialHighlight' is missing. " +
                    "Run Sorolla/Tutorial/Setup Highlight System.");
                return;
            }

            if (_sortingGroup != null)
            {
                _origSortingLayerId = _sortingGroup.sortingLayerID;
                _sortingGroup.sortingLayerName = HighlightSortingLayer;
                _wasElevated = true;
            }
            else if (_spriteRenderer != null)
            {
                _origSortingLayerId = _spriteRenderer.sortingLayerID;
                _spriteRenderer.sortingLayerName = HighlightSortingLayer;
                _wasElevated = true;
            }
        }

        public void Restore()
        {
            if (!_wasElevated) return;
            if (_sortingGroup != null) _sortingGroup.sortingLayerID = _origSortingLayerId;
            else if (_spriteRenderer != null) _spriteRenderer.sortingLayerID = _origSortingLayerId;
            _wasElevated = false;
        }

        private static bool SortingLayerExists(string name)
        {
            var layers = SortingLayer.layers;
            for (int i = 0; i < layers.Length; i++)
                if (layers[i].name == name) return true;
            return false;
        }

        private static Bounds ComputeSortingGroupBounds(SortingGroup group)
        {
            var renderers = group.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(group.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}
