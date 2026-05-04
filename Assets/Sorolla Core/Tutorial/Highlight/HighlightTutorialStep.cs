using NaughtyAttributes;
using UnityEngine;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Animation pattern applied to the pointer inside
    /// <see cref="HighlightTutorialStepPanel"/>.
    /// </summary>
    public enum PointerAnimationMode
    {
        /// <summary>No pointer animation — pointer stays hidden.</summary>
        None = 0,

        /// <summary>Pointer fades in/out over each target in order. Works with 1..N targets.</summary>
        PulseAll = 1,

        /// <summary>Pointer slides between target[0] and target[1] in a loop. Requires exactly 2 targets.</summary>
        DragBetweenPair = 2,

        /// <summary>Pointer walks targets[0] → [1] → … → [n-1] → [0] in a loop. N &gt;= 2.</summary>
        DragAlongPath = 3,
    }

    /// <summary>
    /// Tutorial step that dims the screen, highlights 1..N targets and shows a
    /// message. Pair with the <see cref="HighlightTutorialStepPanel"/> prefab
    /// supplied by Sorolla Core (or a game-local duplicate) as
    /// <see cref="TutorialStepBase.PanelPrefab"/>.
    /// </summary>
    [CreateAssetMenu(
        fileName = "HighlightTutorialStep",
        menuName = "Sorolla/Tutorial/Highlight Step",
        order = 2)]
    public class HighlightTutorialStep : TutorialStepBase
    {
        [Header("Highlight Targets")]
        [Tooltip("Ids of TutorialHighlightTargets to focus on. 1..N supported.")]
        public string[] TargetIds;

        [TextArea(2, 4)]
        [Tooltip("Message displayed near the highlighted element(s).")]
        public string Message;

        [Header("Layout (per-step overrides)")]
        [Tooltip("Offset of the MessageRoot from the group centroid, in canvas pixels. Positive Y = above.")]
        public Vector2 MessageOffset = Vector2.zero;

        [Tooltip("Offset of the Arrow from the group centroid, in canvas pixels. Positive Y = above.")]
        public Vector2 ArrowOffset = Vector2.zero;

        [Header("Pointer")]
        [Tooltip("Animation applied to the pointer graphic. None = no pointer.")]
        public PointerAnimationMode PointerMode = PointerAnimationMode.None;

        [ShowIf(nameof(PointerAnimationEnabled))]
        [Min(0f)]
        [Tooltip("Seconds the pointer takes to travel between targets (or fade cycle duration in PulseAll mode).")]
        public float PointerDuration = 1.2f;

        [ShowIf(nameof(PointerAnimationEnabled))]
        [Min(0f)]
        [Tooltip("Seconds the pointer holds at each target before the next hop / cycle.")]
        public float PointerHoldDuration = 0.35f;

        [ShowIf(nameof(PointerAnimationEnabled))]
        [Min(0f)]
        [Tooltip("Seconds the pointer waits before the first hop / cycle starts.")]
        public float PointerStartDelay = 0.2f;

        [Header("Decoration")]
        [Tooltip("Spawn a ring graphic on every target.")]
        public bool ShowRingOnTargets = true;

        [Tooltip("Override ring sizeDelta per step. (0,0) = use the prefab RingTemplate's size.")]
        public Vector2 RingSize = Vector2.zero;

        [Tooltip("Enable the panel's Arrow RectTransform (position driven by ArrowOffset). Independent of the base TutorialStepBase.ShowArrow, which drives the global TutorialArrowController.")]
        public bool ShowPanelArrow = false;

        // NaughtyAttributes [ShowIf] helper
        private bool PointerAnimationEnabled => PointerMode != PointerAnimationMode.None;
    }
}
