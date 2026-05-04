using System;
using UnityEngine;

namespace Sorolla.Tutorial
{
    [Serializable]
    public struct HideEntry
    {
        [Tooltip("Object to hide/show")]
        public GameObject Object;

        [Tooltip("Reveal this object when playing this level or higher")]
        public int RevealLevel;

        [Tooltip("Reveal when this step index is reached within the level (0 = reveal when level starts)")]
        public int RevealStepInLevel;
    }

    /// <summary>
    /// Reveals/hides GameObjects based on tutorial progress. Self-subscribes to
    /// TutorialController.OnTutorialStepChanged, so it works whether the controller
    /// lives in the level scene or is loaded from a separate bootstrap scene.
    /// Initial state is resolved lazily via ServiceLocator to avoid cross-scene
    /// inspector references and Awake-order races.
    /// </summary>
    public class TutorialObjectsHider : MonoBehaviour
    {
        [Header("Objects to Hide/Show")]
        [SerializeField] private HideEntry[] _objectsToHide;

        private bool _subscribed;

        private void OnEnable()
        {
            if (!_subscribed)
            {
                TutorialController.OnTutorialStepChanged += ApplyStep;
                _subscribed = true;
            }
            ApplyCurrentState();
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                TutorialController.OnTutorialStepChanged -= ApplyStep;
                _subscribed = false;
            }
        }

        /// <summary>
        /// Legacy entry point (still called by TutorialController.BuildTutorial).
        /// OnEnable already handles subscription — this just re-applies current state.
        /// </summary>
        public void Init() => ApplyCurrentState();

        private void ApplyCurrentState()
        {
            var controller = ServiceLocator.Instance?.TryResolve<TutorialController>();
            if (controller != null)
                ApplyStep(controller.CurrentLevel, controller.CurrentStepInLevel);
            else
                ApplyStep(-1, -1); // No controller yet → default to hidden; event will update us on level start.
        }

        public void ApplyStep(int level, int stepInLevel)
        {
            if (_objectsToHide == null) return;

            foreach (var entry in _objectsToHide)
            {
                if (entry.Object == null) continue;

                // Reveals if: playing higher level, OR same level at/past the step
                bool shouldReveal = level > entry.RevealLevel ||
                    (level == entry.RevealLevel && stepInLevel >= entry.RevealStepInLevel);

                entry.Object.transform.localScale = shouldReveal ? Vector3.one : Vector3.zero;
            }
        }
    }
}
