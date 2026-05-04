using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Unified tutorial overlay panel. Dims the screen, elevates every resolved
    /// <see cref="TutorialHighlightTarget"/> above the dim (via its adapter),
    /// positions a ring per target, shows a message near the group centroid and
    /// optionally animates a pointer.
    ///
    /// Reads its config from <see cref="TutorialController.CurrentStep"/> — expects
    /// a <see cref="HighlightTutorialStep"/>. Works for UI and world-space targets
    /// transparently: the adapter abstraction picks the right elevation strategy.
    ///
    /// The panel has NO Canvas of its own. It reparents on enable to
    /// <see cref="TutorialOverlayHost.OverlayParent"/> — a scene-level Canvas
    /// pre-configured in Screen Space - Camera mode. That scene Canvas owns the render
    /// camera, CanvasScaler, sortingLayer, and GraphicRaycaster; the panel is just
    /// content.
    /// </summary>
    public class HighlightTutorialStepPanel : TutorialStepPanel
    {
        [Header("Panel References")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [Tooltip("Root RectTransform of the message box. Re-positioned near the target centroid + MessageOffset.")]
        [SerializeField] private RectTransform _messageRoot;
        [Tooltip("Optional arrow graphic. Enabled when step.ShowPanelArrow is true; positioned via ArrowOffset.")]
        [SerializeField] private RectTransform _arrow;
        [Tooltip("Pointer graphic animated by PointerAnimationMode. Disabled when PointerMode == None.")]
        [SerializeField] private RectTransform _pointer;
        [Tooltip("Disabled RectTransform used as a template; one clone is pooled per target to draw a ring.")]
        [SerializeField] private RectTransform _ringTemplate;
        [Tooltip("Full-screen dim image (Image with raycastTarget = true to block input). Optional.")]
        [SerializeField] private Image _dim;

        [Header("Elevation")]
        [Tooltip("Sorting order applied by UI adapters to elevate UI targets above the dim. Set above the panel Canvas's own order.")]
        [SerializeField] private int _uiTargetSortingOrder = 1100;

        [Header("Late-Registration")]
        [Tooltip("Extra seconds to wait for a target to register after the panel spawns (on top of the step's EntryDelay).")]
        [SerializeField] private float _lateRegistrationGrace = 1f;

        private HighlightTutorialStep _step;
        private Canvas _canvas;
        private Camera _camera;

        private readonly List<ITutorialHighlightable> _resolved = new();
        private readonly HashSet<string> _pendingIds = new();
        private readonly List<RectTransform> _activeRings = new();
        private Sequence _pointerSequence;
        private float _pendingDeadline;
        private bool _waitingForTargets;

        // Designer-placed anchored positions, captured on enable so MessageOffset /
        // ArrowOffset can be applied as POST-offsets. "(0,0)" means "don't move".
        private Vector2 _messagePrefabAnchoredPos;
        private Vector2 _arrowPrefabAnchoredPos;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_messageRoot != null) _messagePrefabAnchoredPos = _messageRoot.anchoredPosition;
            if (_arrow != null) _arrowPrefabAnchoredPos = _arrow.anchoredPosition;
            HideRingTemplate();
            ReparentToOverlayHost();
            CacheCanvasAndCamera();
            ApplyCurrentStep();
            TutorialHighlightTarget.OnTargetRegistered += HandleTargetRegisteredLate;
        }

        /// <summary>
        /// The ring template is a disabled-by-convention child we <c>Instantiate</c>
        /// from. Force it inactive at runtime so it never renders alongside its clones.
        /// </summary>
        private void HideRingTemplate()
        {
            if (_ringTemplate != null && _ringTemplate.gameObject.activeSelf)
                _ringTemplate.gameObject.SetActive(false);
        }

        /// <summary>
        /// Moves the panel from <c>UIManager.PanelsParent</c> (usually a Screen Space -
        /// Overlay chain) to the scene's <see cref="TutorialOverlayHost"/>, which owns
        /// a root Canvas in Screen Space - Camera mode. Sets the RectTransform to fill
        /// the overlay so dim + message positioning work as if the panel itself were
        /// the overlay.
        /// </summary>
        private void ReparentToOverlayHost()
        {
            var host = TutorialOverlayHost.Instance;
            if (host == null)
            {
                Debug.LogError(
                    "[HighlightTutorialStepPanel] No TutorialOverlayHost in the scene. " +
                    "Add a GameObject with a Screen Space - Camera Canvas + CanvasScaler + GraphicRaycaster + TutorialOverlayHost component.",
                    this);
                return;
            }

            var parent = host.OverlayParent;
            if (parent == null) return;

            var rt = transform as RectTransform;
            if (rt == null)
            {
                transform.SetParent(parent, worldPositionStays: false);
                return;
            }

            rt.SetParent(parent, worldPositionStays: false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            TutorialHighlightTarget.OnTargetRegistered -= HandleTargetRegisteredLate;
            TeardownRun();
        }

        private void OnDestroy()
        {
            TeardownRun();
        }

        private void CacheCanvasAndCamera()
        {
            // Resolve the scene-level overlay Canvas (from the TutorialOverlayHost).
            // After ReparentToOverlayHost, the panel lives under this Canvas and
            // inherits its render mode, scale, and camera.
            var parentCanvas = GetComponentInParent<Canvas>();
            _canvas = parentCanvas != null ? parentCanvas.rootCanvas : null;

            if (_canvas == null)
            {
                Debug.LogWarning("[HighlightTutorialStepPanel] Could not resolve a parent Canvas — is TutorialOverlayHost in the scene?", this);
                return;
            }

            if (SortingLayer.NameToID("TutorialHighlight") == 0 && !HasSortingLayer("TutorialHighlight"))
            {
                Debug.LogError(
                    "[HighlightTutorialStepPanel] Sorting layer 'TutorialHighlight' is missing. " +
                    "Run Sorolla/Tutorial/Setup Highlight System.");
            }

            _camera = _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;
        }

        private void ApplyCurrentStep()
        {
            var controller = ServiceLocator.Instance?.TryResolve<TutorialController>();
            if (controller == null)
            {
                Debug.LogWarning("[HighlightTutorialStepPanel] TutorialController unavailable.");
                return;
            }

            if (controller.CurrentStep is not HighlightTutorialStep step)
            {
                Debug.LogWarning("[HighlightTutorialStepPanel] Current step is not a HighlightTutorialStep; panel will render with defaults.");
                return;
            }

            _step = step;

            if (_messageText != null) _messageText.text = step.Message;
            if (_arrow != null) _arrow.gameObject.SetActive(step.ShowPanelArrow);
            if (_pointer != null) _pointer.gameObject.SetActive(step.PointerMode != PointerAnimationMode.None);

            ResolveTargets();
            RefreshLayout();
            RestartPointer();
        }

        // -------- Target resolution --------

        private void ResolveTargets()
        {
            _resolved.Clear();
            _pendingIds.Clear();

            if (_step == null || _step.TargetIds == null) return;

            for (int i = 0; i < _step.TargetIds.Length; i++)
            {
                string id = _step.TargetIds[i];
                if (string.IsNullOrEmpty(id)) continue;

                var target = TutorialHighlightTarget.Find(id);
                if (target != null && target.Adapter != null)
                {
                    AddResolved(target.Adapter);
                }
                else
                {
                    _pendingIds.Add(id);
                }
            }

            if (_pendingIds.Count > 0)
            {
                _waitingForTargets = true;
                _pendingDeadline = Time.realtimeSinceStartup + _lateRegistrationGrace;
            }
        }

        private void AddResolved(ITutorialHighlightable adapter)
        {
            if (adapter == null) return;
            _resolved.Add(adapter);
            adapter.Elevate(_uiTargetSortingOrder);
        }

        private void HandleTargetRegisteredLate(TutorialHighlightTarget target)
        {
            if (target == null || _step == null) return;
            if (!_pendingIds.Contains(target.Id)) return;

            if (target.Adapter != null)
            {
                _pendingIds.Remove(target.Id);
                AddResolved(target.Adapter);
                RefreshLayout();
                RestartPointer();
            }

            if (_pendingIds.Count == 0) _waitingForTargets = false;
        }

        private void Update()
        {
            if (!_waitingForTargets) return;
            if (_pendingIds.Count == 0) { _waitingForTargets = false; return; }
            if (Time.realtimeSinceStartup < _pendingDeadline) return;

            _waitingForTargets = false;
            foreach (var id in _pendingIds)
                Debug.LogWarning($"[HighlightTutorialStepPanel] Target '{id}' never registered — step will render without it.");
            _pendingIds.Clear();
        }

        // -------- Layout --------

        private void RefreshLayout()
        {
            SyncRings();
            PositionMessageAndArrow();
            // Pointer renders last so it sits above rings and message — the ring
            // clones are Instantiated as siblings of RingTemplate and would otherwise
            // end up above the Pointer in the hierarchy.
            if (_pointer != null) _pointer.SetAsLastSibling();
        }

        private void SyncRings()
        {
            // Late-registering targets call RefreshLayout repeatedly. Append rings
            // for new targets instead of destroying and re-instantiating the whole
            // set each time (would be O(N²) instantiates across N late arrivals).
            if (_step == null || !_step.ShowRingOnTargets || _ringTemplate == null)
            {
                for (int i = 0; i < _activeRings.Count; i++)
                    if (_activeRings[i] != null) Destroy(_activeRings[i].gameObject);
                _activeRings.Clear();
                return;
            }

            // Trim if resolved targets shrank (shouldn't happen mid-run, but stay defensive).
            while (_activeRings.Count > _resolved.Count)
            {
                int last = _activeRings.Count - 1;
                if (_activeRings[last] != null) Destroy(_activeRings[last].gameObject);
                _activeRings.RemoveAt(last);
            }

            // Reposition existing rings (target screen positions can drift between calls).
            for (int i = 0; i < _activeRings.Count; i++)
            {
                var adapter = _resolved[i];
                if (adapter == null || _activeRings[i] == null) continue;
                PositionRectAtScreen(_activeRings[i], adapter.GetScreenCenter(_camera));
            }

            // Spawn rings only for newly-resolved targets.
            for (int i = _activeRings.Count; i < _resolved.Count; i++)
            {
                var adapter = _resolved[i];
                if (adapter == null) { _activeRings.Add(null); continue; }

                var ring = Instantiate(_ringTemplate, _ringTemplate.parent);
                ring.name = _ringTemplate.name + "_Instance";
                ring.gameObject.SetActive(true);
                if (_step.RingSize != Vector2.zero)
                    ring.sizeDelta = _step.RingSize;
                PositionRectAtScreen(ring, adapter.GetScreenCenter(_camera));
                _activeRings.Add(ring);
            }
        }

        private void PositionMessageAndArrow()
        {
            // Message and arrow keep their prefab positions. MessageOffset / ArrowOffset
            // are POST-offsets from that prefab anchor — a zero offset means "leave it
            // where the designer placed it". Only rings track the target positions.
            if (_step == null) return;

            if (_messageRoot != null && _step.MessageOffset != Vector2.zero)
                _messageRoot.anchoredPosition = _messagePrefabAnchoredPos + _step.MessageOffset;

            if (_arrow != null && _step.ShowPanelArrow && _step.ArrowOffset != Vector2.zero)
                _arrow.anchoredPosition = _arrowPrefabAnchoredPos + _step.ArrowOffset;
        }

        /// <summary>
        /// Place <paramref name="rect"/> at the given screen-pixel point, projected
        /// into the rect's parent local space. Works for Overlay and Camera canvases.
        /// </summary>
        private void PositionRectAtScreen(RectTransform rect, Vector2 screenPoint)
        {
            if (rect == null || _canvas == null) return;

            Camera screenCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _camera;
            var parent = rect.parent as RectTransform;
            if (parent == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, screenCam, out var local))
                rect.anchoredPosition = local;
        }

        // -------- Pointer animation --------

        private void RestartPointer()
        {
            KillPointerSequence();
            if (_pointer == null || _step == null) return;
            if (_step.PointerMode == PointerAnimationMode.None) return;
            if (_resolved.Count == 0) return;

            switch (_step.PointerMode)
            {
                case PointerAnimationMode.PulseAll:
                    BuildPulseSequence();
                    break;
                case PointerAnimationMode.DragBetweenPair:
                    if (_resolved.Count >= 2) BuildPathSequence(2);
                    else BuildPulseSequence();
                    break;
                case PointerAnimationMode.DragAlongPath:
                    if (_resolved.Count >= 2) BuildPathSequence(_resolved.Count);
                    else BuildPulseSequence();
                    break;
            }
        }

        private void BuildPulseSequence()
        {
            var group = _pointer.GetComponent<CanvasGroup>();
            if (group == null) group = _pointer.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var seq = DOTween.Sequence().SetUpdate(true);
            seq.AppendInterval(_step.PointerStartDelay);

            for (int i = 0; i < _resolved.Count; i++)
            {
                int idx = i;
                seq.AppendCallback(() => PlacePointerAt(_resolved[idx]));
                seq.Append(group.DOFade(1f, _step.PointerDuration * 0.5f).SetUpdate(true));
                seq.AppendInterval(_step.PointerHoldDuration);
                seq.Append(group.DOFade(0f, _step.PointerDuration * 0.5f).SetUpdate(true));
            }

            seq.SetLoops(-1);
            _pointerSequence = seq;
        }

        private void BuildPathSequence(int count)
        {
            PlacePointerAt(_resolved[0]);

            var seq = DOTween.Sequence().SetUpdate(true);
            seq.AppendInterval(_step.PointerStartDelay);

            for (int i = 0; i < count; i++)
            {
                int from = i;
                int to = (i + 1) % count;
                seq.AppendCallback(() => PlacePointerAt(_resolved[from]));
                seq.AppendInterval(_step.PointerHoldDuration);
                seq.Append(_pointer.DOAnchorPos(ComputeAnchoredPos(_pointer, _resolved[to].GetScreenCenter(_camera)),
                                                _step.PointerDuration)
                                                .SetEase(Ease.InOutSine)
                                                .SetUpdate(true));
            }

            seq.SetLoops(-1);
            _pointerSequence = seq;
        }

        private void PlacePointerAt(ITutorialHighlightable target)
        {
            if (_pointer == null || target == null) return;
            PositionRectAtScreen(_pointer, target.GetScreenCenter(_camera));
        }

        private Vector2 ComputeAnchoredPos(RectTransform rect, Vector2 screenPoint)
        {
            if (rect == null || _canvas == null) return Vector2.zero;

            Camera screenCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _camera;
            var parent = rect.parent as RectTransform;
            if (parent == null) return rect.anchoredPosition;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, screenCam, out var local))
                return local;
            return rect.anchoredPosition;
        }

        private void KillPointerSequence()
        {
            if (_pointerSequence != null)
            {
                _pointerSequence.Kill();
                _pointerSequence = null;
            }
        }

        // -------- Teardown --------

        private void TeardownRun()
        {
            KillPointerSequence();

            for (int i = 0; i < _resolved.Count; i++)
                _resolved[i]?.Restore();
            _resolved.Clear();

            _pendingIds.Clear();
            _waitingForTargets = false;

            for (int i = 0; i < _activeRings.Count; i++)
                if (_activeRings[i] != null) Destroy(_activeRings[i].gameObject);
            _activeRings.Clear();
        }

        private static bool HasSortingLayer(string name)
        {
            var layers = SortingLayer.layers;
            for (int i = 0; i < layers.Length; i++)
                if (layers[i].name == name) return true;
            return false;
        }
    }
}
