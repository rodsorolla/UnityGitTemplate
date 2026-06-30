using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sorolla.UI
{
    /// <summary>
    /// A uGUI <see cref="Button"/> with tactile press feedback: the assigned visuals
    /// (e.g. ButtonFront) drop down a few pixels while held and spring back up on
    /// release. The wired On Click() action fires after a tiny delay so the release
    /// motion is actually visible before the panel closes.
    ///
    /// Multiple press targets all play the same down/up motion together. A target
    /// that is inactive at press time is skipped. Drop-in replacement for Button —
    /// existing On Click() listeners are preserved (inherited m_OnClick). All tweens
    /// and the action delay run on unscaled time so the button still works while
    /// gameplay is paused (timeScale = 0).
    /// </summary>
    [AddComponentMenu("UI/Juicy Button")]
    public class JuicyButton : Button
    {
        [Header("Press Feedback")]
        [Tooltip("Visuals that move down on press (e.g. ButtonFront). Inactive ones are skipped. Defaults to this transform if empty.")]
        [SerializeField] private RectTransform[] _pressTargets;
        [Tooltip("How far, in pixels, the targets drop while held.")]
        [SerializeField, Min(0f)] private float _pressOffset = 8f;
        [SerializeField, Min(0f)] private float _downDuration = 0.04f;
        [SerializeField, Min(0f)] private float _upDuration = 0.09f;
        [Tooltip("Delay before the On Click() action fires, so the release is visible.")]
        [SerializeField, Min(0f)] private float _actionDelay = 0.06f;

        private float[] _restY;
        private Tween[] _moveTweens;
        private Tween _actionTween;

        protected override void Awake()
        {
            base.Awake();
            if (_pressTargets == null || _pressTargets.Length == 0)
                _pressTargets = new[] { transform as RectTransform };

            _restY = new float[_pressTargets.Length];
            _moveTweens = new Tween[_pressTargets.Length];
            for (int i = 0; i < _pressTargets.Length; i++)
            {
                if (_pressTargets[i] != null)
                    _restY[i] = _pressTargets[i].anchoredPosition.y;
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            MoveAll(down: true, _downDuration);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            MoveAll(down: false, _upDuration);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            // Release the press visual if the finger slides off while held.
            MoveAll(down: false, _upDuration);
        }

        // Intercept the click so the wired onClick fires AFTER the release motion,
        // not the instant the finger lifts (which would close the panel mid-animation).
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!IsActive() || !IsInteractable()) return;

            _actionTween?.Kill();
            _actionTween = DOVirtual.DelayedCall(_actionDelay, () => onClick.Invoke(), ignoreTimeScale: true);
        }

        private void MoveAll(bool down, float duration)
        {
            if (_pressTargets == null) return;
            for (int i = 0; i < _pressTargets.Length; i++)
            {
                var target = _pressTargets[i];
                if (target == null || !target.gameObject.activeInHierarchy) continue;

                _moveTweens[i]?.Kill();
                float y = down ? _restY[i] - _pressOffset : _restY[i];
                _moveTweens[i] = target.DOAnchorPosY(y, duration).SetUpdate(true);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _actionTween?.Kill();
            // Snap every target back to rest so a re-shown button isn't stuck down.
            // _moveTweens/_restY are allocated in Awake; OnDisable can fire first
            // when the button starts inactive, so guard on the runtime arrays.
            if (_moveTweens == null || _restY == null) return;
            for (int i = 0; i < _pressTargets.Length; i++)
            {
                _moveTweens[i]?.Kill();
                var target = _pressTargets[i];
                if (target == null) continue;
                Vector2 p = target.anchoredPosition;
                p.y = _restY[i];
                target.anchoredPosition = p;
            }
        }
    }
}
