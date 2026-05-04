using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Sorolla.UI.Transitions
{
    /// <summary>
    /// Fade in/out transition using CanvasGroup alpha.
    /// </summary>
    [CreateAssetMenu(fileName = "FadeTransition", menuName = "Sorolla/UI/Transitions/Fade")]
    public class FadeTransition : UITransitionBase
    {
        [Header("Fade Settings")]
        [SerializeField, Range(0f, 1f)] private float _startAlpha = 0f;
        [SerializeField, Range(0f, 1f)] private float _endAlpha = 1f;

        public override async UniTask PlayEnterAsync(Transform target)
        {
            KillExistingTweens(target);

            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = _startAlpha;
            await canvasGroup.DOFade(_endAlpha, _duration)
                .SetEase(_enterEase)
                .SetUpdate(true) // Ignore timeScale
                .AsyncWaitForCompletion();
        }

        public override async UniTask PlayExitAsync(Transform target)
        {
            KillExistingTweens(target);

            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null) return;

            await canvasGroup.DOFade(_startAlpha, _duration)
                .SetEase(_exitEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
        }
    }
}
