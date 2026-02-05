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

    public class TutorialObjectsHider : MonoBehaviour
    {
        [Header("Reference to Tutorial Controller")]
        [SerializeField] private TutorialController _tutorialController;

        [Header("Objects to Hide/Show")]
        [SerializeField] private HideEntry[] _objectsToHide;

        public void Init()
        {
            ApplyStep(_tutorialController.CurrentLevel, _tutorialController.CurrentStepInLevel);
            TutorialController.OnTutorialStepChanged += ApplyStep;
        }

        private void OnDisable()
        {
            TutorialController.OnTutorialStepChanged -= ApplyStep;
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
