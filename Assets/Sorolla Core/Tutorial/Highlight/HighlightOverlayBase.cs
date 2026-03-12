using UnityEngine;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Base class for tutorial highlight overlays. Subclass for world-space or UI-based overlays.
    /// </summary>
    public abstract class HighlightOverlayBase : MonoBehaviour
    {
        public abstract void Show();
        public abstract void Hide();
    }
}
