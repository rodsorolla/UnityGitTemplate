using UnityEngine;

namespace Sorolla
{
    public enum FitMode
    {
        Stretch,  // Fills screen, may distort
        Fit,      // Maintains aspect ratio, may show background on edges
        Fill      // Maintains aspect ratio, may crop edges
    }

    /// <summary>
    /// Scales a quad/plane to fit the camera view at its current distance.
    /// Useful for background elements that need to fill the screen.
    /// </summary>
    public class FitToCamera : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera _targetCamera;

        [Header("Fit Settings")]
        [SerializeField] private FitMode _fitMode = FitMode.Fill;
        [SerializeField] private Vector2 _originalAspect = new Vector2(16f, 9f);
        [SerializeField] private bool _updateEveryFrame;
        [SerializeField] private Vector2 _padding = Vector2.zero;

        private void Start()
        {
            Fit();
        }

        private void LateUpdate()
        {
            if (_updateEveryFrame)
            {
                Fit();
            }
        }

        /// <summary>
        /// Fits the object to the camera view.
        /// </summary>
        public void Fit()
        {
            var cam = _targetCamera;
            if (cam == null)
                return;

            // Calculate distance from camera to quad along camera's forward axis
            Vector3 toQuad = transform.position - cam.transform.position;
            float distance = Mathf.Abs(Vector3.Dot(toQuad, cam.transform.forward));

            // Calculate view size at this distance
            float screenHeight = cam.orthographic
                ? cam.orthographicSize * 2f
                : 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float screenWidth = screenHeight * cam.aspect;

            // Apply fit mode
            float finalWidth = screenWidth;
            float finalHeight = screenHeight;

            if (_fitMode != FitMode.Stretch)
            {
                float originalRatio = _originalAspect.x / _originalAspect.y;
                float screenRatio = screenWidth / screenHeight;

                if (_fitMode == FitMode.Fit)
                {
                    if (screenRatio > originalRatio)
                    {
                        finalHeight = screenHeight;
                        finalWidth = finalHeight * originalRatio;
                    }
                    else
                    {
                        finalWidth = screenWidth;
                        finalHeight = finalWidth / originalRatio;
                    }
                }
                else // Fill
                {
                    if (screenRatio > originalRatio)
                    {
                        finalWidth = screenWidth;
                        finalHeight = finalWidth / originalRatio;
                    }
                    else
                    {
                        finalHeight = screenHeight;
                        finalWidth = finalHeight * originalRatio;
                    }
                }
            }

            // Scale to fit screen
            transform.localScale = new Vector3(finalWidth + _padding.x, finalHeight + _padding.y, 1f);
        }
    }
}
