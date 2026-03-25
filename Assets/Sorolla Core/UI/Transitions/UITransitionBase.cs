using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Sorolla.UI.Transitions
{
    /// <summary>
    /// Base class for DOTween-based UI transitions.
    /// Create ScriptableObject instances to configure and reuse transitions.
    /// </summary>
    public abstract class UITransitionBase : ScriptableObject, IUITransition
    {
        [Header("Timing")]
        [SerializeField, Min(0f)] protected float _duration = 0.3f;

        [Header("Easing")]
        [SerializeField] protected Ease _enterEase = Ease.OutBack;
        [SerializeField] protected Ease _exitEase = Ease.InBack;

        /// <summary>
        /// Duration of the transition in seconds.
        /// </summary>
        public float Duration => _duration;

        /// <summary>
        /// Easing curve for enter transition.
        /// </summary>
        public Ease EnterEase => _enterEase;

        /// <summary>
        /// Easing curve for exit transition.
        /// </summary>
        public Ease ExitEase => _exitEase;

        /// <summary>
        /// Play the enter/show transition animation.
        /// </summary>
        public abstract UniTask PlayEnterAsync(Transform target);

        /// <summary>
        /// Play the exit/hide transition animation.
        /// </summary>
        public abstract UniTask PlayExitAsync(Transform target);

        /// <summary>
        /// Helper to kill any existing tweens on the target before starting a new one.
        /// </summary>
        protected void KillExistingTweens(Transform target)
        {
            target.DOKill();
        }
    }
}
