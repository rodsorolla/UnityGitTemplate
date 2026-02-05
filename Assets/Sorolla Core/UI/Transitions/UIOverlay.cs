using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Sorolla.UI.Transitions
{
    /// <summary>
    /// Screen overlay for fade transitions between screens.
    /// Attach to a Canvas with a full-screen Image and CanvasGroup.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _defaultDuration = 0.3f;
        [SerializeField] private Ease _fadeEase = Ease.Linear;

        private void Awake()
        {
            _canvasGroup ??= GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        /// <summary>
        /// Fade the overlay to fully opaque (blocking).
        /// </summary>
        public async Task FadeInAsync(float? duration = null)
        {
            _canvasGroup.DOKill();
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            await _canvasGroup.DOFade(1f, duration ?? _defaultDuration)
                .SetEase(_fadeEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
        }

        /// <summary>
        /// Fade the overlay to fully transparent (non-blocking).
        /// </summary>
        public async Task FadeOutAsync(float? duration = null)
        {
            _canvasGroup.DOKill();

            await _canvasGroup.DOFade(0f, duration ?? _defaultDuration)
                .SetEase(_fadeEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        /// <summary>
        /// Perform a fade-through transition (fade in, execute action, fade out).
        /// </summary>
        public async Task FadeThroughAsync(System.Func<Task> action, float? duration = null)
        {
            await FadeInAsync(duration);
            if (action != null)
            {
                await action();
            }
            await FadeOutAsync(duration);
        }

        /// <summary>
        /// Immediately set the overlay to a specific alpha without animation.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            _canvasGroup.DOKill();
            _canvasGroup.alpha = alpha;
            _canvasGroup.blocksRaycasts = alpha > 0f;
            _canvasGroup.interactable = alpha > 0f;
        }
    }
}
