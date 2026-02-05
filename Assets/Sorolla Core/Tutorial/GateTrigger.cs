using UnityEngine;

namespace Sorolla.Tutorial
{
    /// <summary>
    /// A simple trigger collider that notifies the TutorialController when the player enters the collider.
    /// ⚠️ The collider must be set as a trigger in the Unity Editor.
    /// </summary>
    
    [RequireComponent(typeof(Collider))]
    public class GateTriggerCollider : MonoBehaviour
    {
        [SerializeField] private string _stepId;
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private string _requiredTag = "Player"; // Only trigger for objects with this tag
        
        private bool _hasTriggered = false;

        private void OnTriggerEnter(Collider other)
        {
            // Check if the collider has the required tag
            if (!string.IsNullOrEmpty(_requiredTag) && !other.CompareTag(_requiredTag))
                return;
            
            if (_triggerOnce && _hasTriggered)
                return;
                
            _hasTriggered = true;
            TutorialController.TriggerGate(_stepId);
            
            if (_triggerOnce)
            {
                // Disable this component after triggering once
                this.enabled = false;
            }
        }
    }
}