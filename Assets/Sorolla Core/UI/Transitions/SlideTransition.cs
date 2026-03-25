using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Sorolla.UI.Transitions
{
    /// <summary>
    /// Direction for slide transitions.
    /// </summary>
    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// Slide in/out transition from a specified direction.
    /// </summary>
    [CreateAssetMenu(fileName = "SlideTransition", menuName = "Sorolla/UI/Transitions/Slide")]
    public class SlideTransition : UITransitionBase
    {
        [Header("Slide Settings")]
        [SerializeField] private SlideDirection _direction = SlideDirection.Right;
        [SerializeField] private float _offset = 1000f;

        public override async UniTask PlayEnterAsync(Transform target)
        {
            KillExistingTweens(target);

            var rectTransform = target as RectTransform;
            if (rectTransform == null)
            {
                Debug.LogWarning("SlideTransition requires a RectTransform");
                return;
            }

            var startPos = rectTransform.anchoredPosition;
            var offsetVector = GetOffsetVector();
            rectTransform.anchoredPosition = startPos + offsetVector;

            await rectTransform.DOAnchorPos(startPos, _duration)
                .SetEase(_enterEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
        }

        public override async UniTask PlayExitAsync(Transform target)
        {
            KillExistingTweens(target);

            var rectTransform = target as RectTransform;
            if (rectTransform == null) return;

            var endPos = rectTransform.anchoredPosition + GetOffsetVector();

            await rectTransform.DOAnchorPos(endPos, _duration)
                .SetEase(_exitEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
        }

        private Vector2 GetOffsetVector()
        {
            return _direction switch
            {
                SlideDirection.Left => new Vector2(-_offset, 0f),
                SlideDirection.Right => new Vector2(_offset, 0f),
                SlideDirection.Up => new Vector2(0f, _offset),
                SlideDirection.Down => new Vector2(0f, -_offset),
                _ => Vector2.zero
            };
        }
    }
}
