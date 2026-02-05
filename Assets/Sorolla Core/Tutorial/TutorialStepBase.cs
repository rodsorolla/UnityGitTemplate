using System;
using NaughtyAttributes;
using UnityEngine;

namespace Sorolla.Tutorial
{
    /// <summary>
    /// Defines how a tutorial step can be completed.
    /// </summary>
    public enum TutorialStepCompletionMode
    {
        Manual,     // Completed by button click or explicit Complete() call
        Event,      // Completed when specific event ID is triggered
        Timed       // Auto-completes after AutoCompleteDelay seconds
    }

    /// <summary>
    /// Defines how a tutorial step entry is triggered.
    /// </summary>
    public enum TutorialStepEntryMode
    {
        Immediate,  // Step enters immediately when reached in sequence
        Gate        // Step waits for a GateTriggerCollider with matching Id
    }

    [CreateAssetMenu(fileName = "TutorialStep", menuName = "Sorolla/Tutorial/Tutorial Step", order = 1)]
    public class TutorialStepBase : ScriptableObject
    {
        // Hook for side-effects
        public Action OnEnter;
        public Action OnExit;

        [Header("Tutorial Step Settings")]
        public string Id = "step_id";

        [Header("Completion Mode")]
        [Tooltip("How this step can be completed:\n• Manual: Button click or Complete() call\n• Event: CompleteStep(Id) call\n• Timed: Auto-completes after delay")]
        public TutorialStepCompletionMode CompletionMode = TutorialStepCompletionMode.Manual;

        [ShowIf(nameof(CompletionMode), TutorialStepCompletionMode.Timed)]
        [Tooltip("Time in seconds before the step auto-completes (after entering)")]
        [Min(0)] public float AutoCompleteDelay = 2f;

        [BoxGroup("Entry Settings")]
        [Tooltip("How this step entry is triggered:\n• Immediate: Enters when reached in sequence\n• Gate: Waits for GateTriggerCollider with matching Id")]
        public TutorialStepEntryMode EntryMode = TutorialStepEntryMode.Immediate;
        
        [BoxGroup("Entry Settings")]
        [Tooltip("Delay in seconds before the step enters (after being triggered). 0 = immediate entry.")]
        [Min(0)] public float EntryDelay = 0f;

        [BoxGroup("Gameplay Settings")]
        public bool PauseGameplayDuringStep = false;
        [BoxGroup("Gameplay Settings")]
        public bool FreezePlayer = false;

        [BoxGroup("UI Settings")]
        [Tooltip("Panel prefab to instantiate for this step. If null, no panel is shown.")]
        public GameObject PanelPrefab;

        [Header("Arrow Settings")]
        public bool ShowArrow = false;
        [ShowIf("ShowArrow")]
        public Vector3 ArrowWorldPos;
        [ShowIf("ShowArrow")]
        public Vector3 ArrowPointTo;
        [ShowIf("ShowArrow")]
        public bool ArrowFollowTarget = false;

        /// <summary>
        /// Returns true if this step can be completed by a manual Complete() call.
        /// </summary>
        public bool CanCompleteManually()
        {
            return CompletionMode == TutorialStepCompletionMode.Manual;
        }

        /// <summary>
        /// Returns true if this step can be completed by the given event ID.
        /// </summary>
        public bool CanCompleteByEvent(string eventId)
        {
            return CompletionMode == TutorialStepCompletionMode.Event &&
                   !string.IsNullOrEmpty(Id) &&
                   Id == eventId;
        }

        /// <summary>
        /// Returns true if this step auto-completes after a delay.
        /// </summary>
        public bool IsAutoComplete => CompletionMode == TutorialStepCompletionMode.Timed;

        /// <summary>
        /// Gets the delay before the step enters (0 if no delay).
        /// </summary>
        public float GetEntryDelay() => EntryDelay;

        /// <summary>
        /// Gets the delay before auto-completion (only valid for Timed mode).
        /// </summary>
        public float GetAutoCompleteDelay() => IsAutoComplete ? AutoCompleteDelay : 0f;
    }
}
