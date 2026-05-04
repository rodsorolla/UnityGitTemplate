using UnityEngine;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Scene-level host for tutorial highlight panels. Place this on a GameObject that
    /// carries a root Canvas pre-configured for highlight overlays:
    /// <list type="bullet">
    ///   <item>Canvas — Screen Space - Camera, Render Camera wired, sortingLayer "Sky" (or any layer rendering above world sprites but below TutorialHighlight), Order 1000.</item>
    ///   <item>CanvasScaler — matches the game's UI resolution (e.g., 1080x1920).</item>
    ///   <item>GraphicRaycaster — so the dim can block input.</item>
    /// </list>
    ///
    /// <see cref="HighlightTutorialStepPanel"/> finds this host on enable and reparents
    /// itself here, bypassing <c>UIManager.PanelsParent</c> (which typically lives in a
    /// Screen Space - Overlay chain and would defeat the "dim between sprites" design).
    /// </summary>
    public class TutorialOverlayHost : MonoBehaviour
    {
        public static TutorialOverlayHost Instance { get; private set; }

        [SerializeField] private RectTransform _overlayParent;

        /// <summary>
        /// The RectTransform that panels reparent to. Defaults to this GameObject's
        /// RectTransform when <c>_overlayParent</c> is unassigned.
        /// </summary>
        public RectTransform OverlayParent => _overlayParent != null ? _overlayParent : (RectTransform)transform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
