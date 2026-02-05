using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.FTX
{
    /// <summary>
    /// Component to attach to hint GameObjects for first-time experiences.
    /// Automatically hides if the key has been seen before.
    /// </summary>
    public class FirstTimeHint : MonoBehaviour
    {
        [SerializeField] private string _key;

        [Header("Mark As Seen")]
        [Tooltip("Mark as seen immediately when this hint is shown")]
        [SerializeField] private bool _markSeenOnEnable;

        [Tooltip("Optional button that marks as seen when clicked")]
        [SerializeField] private Button _dismissButton;

        private IFirstTimeExperienceService _ftxService;
        private bool _markedThisSession;
        private int _instantiatedFrame = -1;

        private void Awake()
        {
            _instantiatedFrame = Time.frameCount;
            _markedThisSession = false;
        }

        private void OnEnable()
        {
            Debug.Log($"[FirstTimeHint] '{_key}' - OnEnable (frame={Time.frameCount}, instantiatedFrame={_instantiatedFrame}, markedThisSession={_markedThisSession})");

            // Skip the initial OnEnable during Instantiate - wait for actual ShowAsync
            if (Time.frameCount == _instantiatedFrame)
            {
                Debug.Log($"[FirstTimeHint] '{_key}' - skipping instantiation frame");
                return;
            }

            _ftxService ??= ServiceLocator.Instance.TryResolve<IFirstTimeExperienceService>();

            if (_ftxService == null)
            {
                // Service not ready yet, try again next frame
                Debug.Log($"[FirstTimeHint] '{_key}' - service null, scheduling retry");
                Invoke(nameof(CheckAndHide), 0.1f);
                return;
            }

            CheckAndHide();
        }

        private void Start()
        {
            if (_dismissButton != null)
            {
                _dismissButton.onClick.AddListener(MarkAsSeen);
            }
        }

        private void OnDestroy()
        {
            if (_dismissButton != null)
            {
                _dismissButton.onClick.RemoveListener(MarkAsSeen);
            }
        }

        private void CheckAndHide()
        {
            // If we already marked as seen this session, stay hidden
            if (_markedThisSession)
            {
                Debug.Log($"[FirstTimeHint] '{_key}' - hiding (marked this session)");
                gameObject.SetActive(false);
                return;
            }

            _ftxService ??= ServiceLocator.Instance.TryResolve<IFirstTimeExperienceService>();

            if (_ftxService == null)
            {
                Debug.LogWarning($"[FirstTimeHint] '{_key}' - service not available, will retry");
                return;
            }

            bool hasSeen = _ftxService.HasSeen(_key);
            Debug.Log($"[FirstTimeHint] '{_key}' - hasSeen={hasSeen}, markOnEnable={_markSeenOnEnable}");

            if (hasSeen)
            {
                Debug.Log($"[FirstTimeHint] '{_key}' - hiding (already seen)");
                gameObject.SetActive(false);
                return;
            }

            if (_markSeenOnEnable)
            {
                Debug.Log($"[FirstTimeHint] '{_key}' - marking as seen");
                _ftxService.MarkAsSeen(_key);
                _markedThisSession = true;
            }
        }

        /// <summary>
        /// Manually mark this hint as seen and hide it.
        /// Called automatically if _dismissButton is assigned.
        /// Can also be called from code or UnityEvents.
        /// </summary>
        public void MarkAsSeen()
        {
            _ftxService ??= ServiceLocator.Instance.TryResolve<IFirstTimeExperienceService>();
            _ftxService?.MarkAsSeen(_key);
            gameObject.SetActive(false);
        }
    }
}
