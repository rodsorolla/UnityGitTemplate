using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Locks the Y-axis rotation of this transform relative to world space,
    /// even when the parent rotates on the Y axis.
    /// Useful for keeping child objects oriented correctly regardless of parent rotation.
    /// </summary>
    public class LockChildYRotation : MonoBehaviour
    {
        [Header("Rotation Lock Settings")]
        [Tooltip("The world Y rotation to maintain (in degrees)")]
        [SerializeField] private float targetWorldYRotation = 0f;
        
        [Tooltip("If true, captures the initial world Y rotation on Start")]
        [SerializeField] private bool useInitialRotation = true;
        
        [Tooltip("If true, smoothly interpolates to the target rotation")]
        [SerializeField] private bool smoothRotation = false;
        
        [Tooltip("Speed of rotation interpolation (only used if smoothRotation is true)")]
        [SerializeField] private float rotationSpeed = 10f;

        private void Start()
        {
            if (useInitialRotation)
            {
                targetWorldYRotation = transform.eulerAngles.y;
            }
        }

        /// <summary>
        /// Locks the Y rotation in LateUpdate to ensure it runs after parent transformations.
        /// </summary>
        private void LateUpdate()
        {
            LockYRotation();
        }

        /// <summary>
        /// Locks the Y-axis rotation to the target world rotation.
        /// </summary>
        private void LockYRotation()
        {
            Vector3 currentEuler = transform.eulerAngles;
            
            if (smoothRotation)
            {
                // Smoothly interpolate to target Y rotation
                float currentY = currentEuler.y;
                float newY = Mathf.LerpAngle(currentY, targetWorldYRotation, Time.deltaTime * rotationSpeed);
                transform.eulerAngles = new Vector3(currentEuler.x, newY, currentEuler.z);
            }
            else
            {
                // Instantly set to target Y rotation
                transform.eulerAngles = new Vector3(currentEuler.x, targetWorldYRotation, currentEuler.z);
            }
        }

        /// <summary>
        /// Sets a new target world Y rotation.
        /// </summary>
        /// <param name="newYRotation">The new Y rotation in degrees</param>
        public void SetTargetYRotation(float newYRotation)
        {
            targetWorldYRotation = newYRotation;
        }

        /// <summary>
        /// Captures the current world Y rotation as the new target.
        /// </summary>
        public void CaptureCurrentYRotation()
        {
            targetWorldYRotation = transform.eulerAngles.y;
        }
    }
}
