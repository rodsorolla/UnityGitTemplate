using UnityEngine;

namespace Sorolla
{
    public class Billboard : MonoBehaviour
    {
        private Camera mainCamera;

        [Header("Billboard Settings")] 
        [Tooltip("If true, the object will only rotate on the Y axis")]
        public bool lockYAxis = false;

        void Start()
        {
            mainCamera = Camera.main;
        }

        /// <summary>
        /// Makes the object face the camera every frame.
        /// </summary>
        void LateUpdate()
        {
            if (mainCamera == null)
                return;

            if (lockYAxis)
            {
                // Only rotate on Y axis (useful for ground-based UI)
                Vector3 directionToCamera = mainCamera.transform.position - transform.position;
                directionToCamera.y = 0;
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
            else
            {
                // Full rotation to face camera
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }
        }
    }
}