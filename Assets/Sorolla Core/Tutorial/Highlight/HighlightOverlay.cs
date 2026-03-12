using UnityEngine;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// World-space dark quad overlay for tutorial highlighting.
    /// Best for top-down or fixed-angle cameras where a positioned quad covers the scene.
    /// </summary>
    public class HighlightOverlay : HighlightOverlayBase
    {
        [Header("Settings")]
        [SerializeField] private Material _overlayMaterial;
        [Tooltip("Y position of the overlay in world space (for top-down camera)")]
        [SerializeField] private float _overlayY = 3f;
        [SerializeField] private Vector2 _quadSize = new Vector2(20f, 20f);

        private GameObject _overlayQuad;

        private void Awake()
        {
            CreateOverlay();
            Hide();
        }

        private void CreateOverlay()
        {
            _overlayQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _overlayQuad.name = "HighlightOverlayQuad";
            _overlayQuad.transform.SetParent(transform);

            // Remove collider
            var collider = _overlayQuad.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            // Apply material
            if (_overlayMaterial != null)
            {
                var renderer = _overlayQuad.GetComponent<Renderer>();
                renderer.material = _overlayMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            PositionOverlay();
        }

        private void PositionOverlay()
        {
            // Position at this GameObject's X/Z with configured Y
            _overlayQuad.transform.position = new Vector3(
                transform.position.x,
                _overlayY,
                transform.position.z
            );
            _overlayQuad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _overlayQuad.transform.localScale = new Vector3(_quadSize.x, _quadSize.y, 1f);
        }

        public override void Show()
        {
            PositionOverlay();
            _overlayQuad.SetActive(true);
        }

        public override void Hide()
        {
            _overlayQuad.SetActive(false);
        }
    }
}
