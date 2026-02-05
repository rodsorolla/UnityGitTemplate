using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Sorolla.UI.Transitions
{
    /// <summary>
    /// Scale pop/bounce transition.
    /// </summary>
    [CreateAssetMenu(fileName = "ScaleTransition", menuName = "Sorolla/UI/Transitions/Scale")]
    public class ScaleTransition : UITransitionBase
    {
        [Header("Scale Settings")]
        [SerializeField, Min(0f)] private float _startScale = 0f;
        [SerializeField, Min(0f)] private float _endScale = 1f;

        public override async Task PlayEnterAsync(Transform target)
        {
            KillExistingTweens(target);

            target.localScale = Vector3.one * _startScale;
            await target.DOScale(_endScale, _duration)
                .SetEase(_enterEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
        }

        public override async Task PlayExitAsync(Transform target)
        {
            KillExistingTweens(target);

            await target.DOScale(_startScale, _duration)
                .SetEase(_exitEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
        }
    }
}
