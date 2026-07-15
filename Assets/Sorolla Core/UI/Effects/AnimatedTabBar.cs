using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.UI.Effects
{
    /// <summary>
    /// Animates a bottom tab bar built on a <see cref="HorizontalLayoutGroup"/> (Control
    /// Child Size + Child Force Expand). The selected button grows a configurable amount
    /// wider than the others by tweening its <see cref="LayoutElement.flexibleWidth"/> —
    /// the setter dirties the layout itself, so the HorizontalLayoutGroup rebuilds smoothly
    /// each frame. Each tab may assign an optional "selected content" child that scales up
    /// while its tab is selected. Fully generic uGUI — no game-specific dependencies.
    /// </summary>
    [DisallowMultipleComponent]
    public class AnimatedTabBar : MonoBehaviour
    {
        [Serializable]
        public class Tab
        {
            [Tooltip("The tab button. A LayoutElement is added at runtime if missing.")]
            public Button Button;
            [Tooltip("Optional child that scales up while this tab is selected.")]
            public Transform SelectedContent;
        }

        [Header("Tabs")]
        [SerializeField] private List<Tab> _tabs = new List<Tab>();

        [Header("Animation")]
        [Tooltip("flexibleWidth of the selected tab. All other tabs stay at 1.")]
        [SerializeField, Min(1f)] private float _selectedWidthMultiplier = 1.2f;
        [Tooltip("Scale applied to a tab's selected content while selected.")]
        [SerializeField, Min(1f)] private float _selectedContentScale = 1.15f;
        [SerializeField, Min(0f)] private float _duration = 0.25f;
        [SerializeField] private Ease _ease = Ease.OutBack;

        [Header("Initial State")]
        [Tooltip("Tab selected instantly (no tween) on enable.")]
        [SerializeField, Min(0)] private int _initialIndex = 0;

        /// <summary>Fired when the selected tab changes. Argument is the tab index.</summary>
        public event Action<int> OnTabSelected;

        private LayoutElement[] _layoutElements;
        private Vector3[] _contentBaseScales;
        private Tween[] _widthTweens;
        private Tween[] _scaleTweens;
        private int _selectedIndex = -1;

        private void Awake()
        {
            int count = _tabs.Count;
            _layoutElements = new LayoutElement[count];
            _contentBaseScales = new Vector3[count];
            _widthTweens = new Tween[count];
            _scaleTweens = new Tween[count];

            for (int i = 0; i < count; i++)
            {
                Tab tab = _tabs[i];
                if (tab.Button != null)
                {
                    LayoutElement le = tab.Button.GetComponent<LayoutElement>();
                    if (le == null) le = tab.Button.gameObject.AddComponent<LayoutElement>();
                    le.flexibleWidth = 1f;
                    _layoutElements[i] = le;

                    int index = i;
                    tab.Button.onClick.AddListener(() => Select(index));
                }
                if (tab.SelectedContent != null)
                    _contentBaseScales[i] = tab.SelectedContent.localScale;
            }
        }

        private void OnEnable()
        {
            // Re-apply the initial selection instantly so re-enabling never drifts.
            _selectedIndex = -1;
            SelectInstant(Mathf.Clamp(_initialIndex, 0, Mathf.Max(0, _tabs.Count - 1)));
        }

        private void OnDisable()
        {
            KillTweens();
        }

        /// <summary>Selects a tab with animation. Re-selecting the current tab is a no-op.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= _tabs.Count || index == _selectedIndex) return;

            int previous = _selectedIndex;
            _selectedIndex = index;

            if (previous >= 0) Animate(previous, selected: false);
            Animate(index, selected: true);

            OnTabSelected?.Invoke(index);
        }

        private void SelectInstant(int index)
        {
            if (_tabs.Count == 0) return;
            _selectedIndex = index;
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool selected = i == index;
                if (_layoutElements[i] != null)
                    _layoutElements[i].flexibleWidth = selected ? _selectedWidthMultiplier : 1f;
                if (_tabs[i].SelectedContent != null)
                    _tabs[i].SelectedContent.localScale = selected
                        ? _contentBaseScales[i] * _selectedContentScale
                        : _contentBaseScales[i];
            }
        }

        private void Animate(int index, bool selected)
        {
            LayoutElement le = _layoutElements[index];
            if (le != null)
            {
                _widthTweens[index]?.Kill();
                float targetWidth = selected ? _selectedWidthMultiplier : 1f;
                _widthTweens[index] = DOTween
                    .To(() => le.flexibleWidth, x => le.flexibleWidth = x, targetWidth, _duration)
                    .SetEase(_ease);
            }

            Transform content = _tabs[index].SelectedContent;
            if (content != null)
            {
                _scaleTweens[index]?.Kill();
                Vector3 targetScale = selected
                    ? _contentBaseScales[index] * _selectedContentScale
                    : _contentBaseScales[index];
                _scaleTweens[index] = content.DOScale(targetScale, _duration).SetEase(_ease);
            }
        }

        private void KillTweens()
        {
            if (_widthTweens == null) return;
            for (int i = 0; i < _widthTweens.Length; i++)
            {
                _widthTweens[i]?.Kill();
                _scaleTweens[i]?.Kill();
            }
        }
    }
}
