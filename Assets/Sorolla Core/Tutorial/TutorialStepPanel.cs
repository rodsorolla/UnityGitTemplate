using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tutorial
{
    /// <summary>
    /// Base class for tutorial step panels. Provides a button that completes the step when clicked.
    /// Inherit from this class to create game-specific tutorial panels.
    /// </summary>
    public abstract class TutorialStepPanel : MonoBehaviour
    {
        [Header("Base Panel Settings")]
        [SerializeField] protected Button _completeButton;

        protected virtual void OnEnable()
        {
            if (_completeButton != null)
                _completeButton.onClick.AddListener(OnCompleteClicked);
        }

        protected virtual void OnDisable()
        {
            if (_completeButton != null)
                _completeButton.onClick.RemoveListener(OnCompleteClicked);
        }

        protected virtual void OnCompleteClicked()
        {
            TutorialController.Complete();
        }
    }
}
