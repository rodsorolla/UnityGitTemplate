using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Marks a GameObject as a tutorial highlight target. Drop this component on a
    /// UI button, world sprite, or any GameObject you want a <see cref="HighlightTutorialStep"/>
    /// to focus on. The right adapter is picked automatically on <c>Awake</c>:
    ///
    /// <list type="bullet">
    /// <item><description>Canvas + GraphicRaycaster → <see cref="UIHighlightAdapter"/></description></item>
    /// <item><description><see cref="SortingGroup"/> → <see cref="SpriteHighlightAdapter"/></description></item>
    /// <item><description>Bare <see cref="SpriteRenderer"/> → <see cref="SpriteHighlightAdapter"/></description></item>
    /// <item><description>none of the above → warns; target registers but <c>Elevate</c> is a no-op</description></item>
    /// </list>
    ///
    /// Targets self-register on <c>OnEnable</c> and unregister on <c>OnDisable</c>.
    /// For dynamic spawn scenarios call <see cref="SetId"/> after adding the
    /// component — it re-registers under the new id and fires
    /// <see cref="OnTargetRegistered"/> so waiting panels attach immediately.
    /// </summary>
    [DisallowMultipleComponent]
    public class TutorialHighlightTarget : MonoBehaviour
    {
        [SerializeField] private string _id;

        /// <summary>Id this target is currently registered under.</summary>
        public string Id => _id;

        /// <summary>Rect transform convenience accessor (UI targets).</summary>
        public RectTransform RectTransform => transform as RectTransform;

        /// <summary>The adapter selected for this target. Null until <c>Awake</c>.</summary>
        public ITutorialHighlightable Adapter => _adapter;

        private ITutorialHighlightable _adapter;
        private bool _registered;

        // -------- Static registry --------

        private static readonly Dictionary<string, TutorialHighlightTarget> _registry = new();

        /// <summary>Fired when a target registers (or re-registers under a new id).</summary>
        public static event Action<TutorialHighlightTarget> OnTargetRegistered;

        /// <summary>Look up a registered target by id.</summary>
        public static TutorialHighlightTarget Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _registry.TryGetValue(id, out var target) ? target : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _registry.Clear();
            OnTargetRegistered = null;
        }

        // -------- Unity lifecycle --------

        private void Awake()
        {
            _adapter = BuildAdapter(_id);
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        // -------- Public API --------

        /// <summary>
        /// Change this target's id at runtime. Safe to call repeatedly; idempotent
        /// when <paramref name="id"/> equals the current id. Fires
        /// <see cref="OnTargetRegistered"/> when the new id is non-empty so panels
        /// already waiting on this id attach.
        /// </summary>
        public void SetId(string id)
        {
            if (_id == id && _registered) return;

            // If the current adapter was elevated (e.g. a panel is still showing and
            // called SetId to re-target us), restore its visual state before we throw
            // the adapter away — otherwise the target stays stuck on TutorialHighlight.
            _adapter?.Restore();

            Unregister();
            _id = id;

            // Rebuild the adapter so the new id is plumbed through.
            _adapter = BuildAdapter(_id);

            if (isActiveAndEnabled)
                Register();
        }

        // -------- Internals --------

        private void Register()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[TutorialHighlightTarget] '{name}' has no id set.", this);
                return;
            }

            _registry[_id] = this;
            _registered = true;
            OnTargetRegistered?.Invoke(this);
        }

        private void Unregister()
        {
            if (!_registered) return;
            _registered = false;

            if (!string.IsNullOrEmpty(_id)
                && _registry.TryGetValue(_id, out var current)
                && current == this)
            {
                _registry.Remove(_id);
            }
        }

        private ITutorialHighlightable BuildAdapter(string id)
        {
            // UI path: Canvas + GraphicRaycaster on this GameObject.
            var canvas = GetComponent<Canvas>();
            var raycaster = GetComponent<GraphicRaycaster>();
            var rectTransform = transform as RectTransform;
            if (canvas != null && raycaster != null && rectTransform != null)
                return new UIHighlightAdapter(id, rectTransform, canvas, raycaster);

            // Sprite path: SortingGroup or a SpriteRenderer in the hierarchy.
            var sortingGroup = GetComponent<SortingGroup>() ?? GetComponentInChildren<SortingGroup>(true);
            if (sortingGroup != null)
                return new SpriteHighlightAdapter(id, sortingGroup);

            var spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer != null)
                return new SpriteHighlightAdapter(id, spriteRenderer);

            Debug.LogWarning(
                $"[TutorialHighlightTarget] '{name}' has no UI (Canvas+GraphicRaycaster), SortingGroup or SpriteRenderer — Elevate/Restore will be no-ops.",
                this);
            return null;
        }
    }
}
