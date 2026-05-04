using UnityEngine;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Abstraction over "a thing a tutorial step can point at". Different rendering
    /// paths (Canvas UI, world-space SpriteRenderer, SortingGroup stack) supply their
    /// own adapter. The panel only knows about this interface.
    /// </summary>
    public interface ITutorialHighlightable
    {
        /// <summary>
        /// Id this target is registered under. Used by the panel to resolve targets
        /// declared by <see cref="HighlightTutorialStep.TargetIds"/>.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The GameObject backing this target. May be used by callers for tag/layer
        /// checks; adapters should not assume the GameObject is still alive.
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// Screen-pixel anchor point used to position the ring / message / pointer.
        /// Adapters own the conversion from their native coord space (world meters for
        /// sprites, pseudo-world pixels for Overlay UI, canvas-camera world for
        /// ScreenSpaceCamera UI). Returning a screen point removes ambiguity for the
        /// panel, which projects via <c>ScreenPointToLocalPointInRectangle</c>.
        /// </summary>
        Vector2 GetScreenCenter(Camera worldCamera);

        /// <summary>
        /// Screen-pixel bounds of the target. Used by the panel to size the ring.
        /// </summary>
        Rect GetScreenBounds(Camera worldCamera);

        /// <summary>
        /// Temporarily push this target above the dim. For UI adapters
        /// <paramref name="highlightLayer"/> is the sortingOrder to apply to the
        /// target Canvas (above the panel Canvas). For sprite adapters it is unused —
        /// they swap sorting layer to <c>TutorialHighlight</c>.
        /// </summary>
        void Elevate(int highlightLayer);

        /// <summary>
        /// Restore whatever state <see cref="Elevate"/> mutated. Must be safe to call
        /// even if the target's renderer was destroyed mid-step.
        /// </summary>
        void Restore();
    }
}
