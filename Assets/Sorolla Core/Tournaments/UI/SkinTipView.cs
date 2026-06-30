using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tournaments.UI
{
    /// Single shared "skin tip" bubble for the tournament tier strip. Repositioned over whichever
    /// cup tier was tapped and pops in (scale 0->1, OutBack) showing the skin that unlocks at that
    /// tier. View-only — TournamentScreen drives it. Prefab authored by the user (SkinTip).
    public class SkinTipView : MonoBehaviour
    {
        [Tooltip("The bubble body that is shown/hidden, repositioned and scaled. Usually this GO's own RectTransform.")]
        [SerializeField] private RectTransform _root;

        [Tooltip("Image that displays the unlocking skin's icon.")]
        [SerializeField] private Image _skinIcon;

        [Tooltip("World-space offset applied to the bubble relative to the tapped cup.")]
        [SerializeField] private Vector2 _worldOffset = Vector2.zero;

        [Header("Pop animation")]
        [SerializeField] private float _popDuration = 0.25f;
        [SerializeField] private Ease _popEase = Ease.OutBack;

        [Tooltip("Auto-close the bubble this many seconds after it is shown. 0 = stay open.")]
        [SerializeField] private float _autoCloseDelay = 3f;

        private Tween _popTween;
        private Tween _autoCloseTween;

        private void Awake()
        {
            Hide();
        }

        /// Positions the bubble over <paramref name="cup"/>, sets its icon and pops it in.
        public void ShowFor(RectTransform cup, Sprite icon)
        {
            if (_root == null || cup == null) return;

            if (_skinIcon != null) _skinIcon.sprite = icon;

            // Activate BEFORE positioning: writing a world position to a RectTransform whose
            // hierarchy was inactive uses a stale parent matrix on the first show, landing the
            // bubble off the cup. (Same gotcha as ChestRewardBubble.)
            _root.gameObject.SetActive(true);
            // Lift the bubble 110px above the cup so it clears the tier art.
            _root.position = cup.position + (Vector3)_worldOffset + new Vector3(0f, 110f, 0f);

            _popTween?.Kill();
            _root.localScale = Vector3.zero;
            _popTween = _root.DOScale(1f, _popDuration)
                .SetEase(_popEase)
                .SetUpdate(true)   // menu may run at timeScale 0
                .SetLink(_root.gameObject);

            _autoCloseTween?.Kill();
            if (_autoCloseDelay > 0f)
            {
                // ignoreTimeScale: menu may run at timeScale 0.
                _autoCloseTween = DOVirtual.DelayedCall(_autoCloseDelay, Hide, ignoreTimeScale: true)
                    .SetLink(_root.gameObject);
            }
        }

        public void Hide()
        {
            _popTween?.Kill();
            _autoCloseTween?.Kill();
            if (_root != null) _root.gameObject.SetActive(false);
        }
    }
}
