using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Configuration for which items to highlight during a tutorial step.
    /// </summary>
    [Serializable]
    public class HighlightConfig
    {
        [Tooltip("The step ID to match (from TutorialStepBase.Id)")]
        public string StepId;

        [Tooltip("Type IDs to highlight when this step is active (matched against IHighlightable.HighlightTypeId)")]
        public string[] HighlightTypeIds;
    }

    /// <summary>
    /// Base class for managing tutorial highlighting using a camera-based approach.
    /// Highlighted items are moved to a separate layer and rendered by an overlay camera.
    /// Extend this class in game-specific code to wire up dependencies and add custom logic.
    /// </summary>
    public abstract class HighlightManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected HighlightOverlayBase _overlay;
        [SerializeField] protected Camera _mainCamera;
        [SerializeField] protected Camera _highlightCamera;

        [Header("Layer Configuration")]
        [Tooltip("Layer for highlighted items (must be created in Tags & Layers)")]
        [SerializeField] protected string _highlightLayerName = "TutorialHighlight";

        // State
        protected Dictionary<string, HighlightConfig> _configByStepId = new();
        protected Dictionary<IHighlightable, List<(GameObject go, int layer)>> _originalLayers = new();
        protected List<IHighlightable> _highlightedItems = new();
        protected int _highlightLayer = -1;
        protected int _originalMainCameraCullingMask;
        protected bool _isHighlightActive;

        // Dependencies (set by subclasses)
        protected IHighlightableProvider _highlightableProvider;
        protected IInputLayerOverride _inputLayerOverride;

        protected virtual void Awake()
        {
            CacheHighlightLayer();
            SetupHighlightCamera();
        }

        protected virtual void OnEnable()
        {
            TutorialController.OnTutorialStepEntered += OnTutorialStepEntered;
            TutorialController.OnTutorialStepChanged += OnTutorialStepChanged;
        }

        protected virtual void OnDisable()
        {
            TutorialController.OnTutorialStepEntered -= OnTutorialStepEntered;
            TutorialController.OnTutorialStepChanged -= OnTutorialStepChanged;
            ClearHighlights();
        }

        /// <summary>
        /// Register a highlight config for a step.
        /// </summary>
        protected void RegisterConfig(string stepId, HighlightConfig config)
        {
            if (!string.IsNullOrEmpty(stepId))
            {
                _configByStepId[stepId] = config;
            }
        }

        protected void CacheHighlightLayer()
        {
            _highlightLayer = LayerMask.NameToLayer(_highlightLayerName);
            if (_highlightLayer == -1)
            {
                Debug.LogError($"[HighlightManager] Layer '{_highlightLayerName}' not found. Please create it in Tags & Layers.");
            }
        }

        protected void SetupHighlightCamera()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_highlightCamera == null)
            {
                Debug.LogError("[HighlightManager] Highlight camera not assigned.");
                return;
            }

            // Initially disable highlight camera
            _highlightCamera.gameObject.SetActive(false);

            // Configure as overlay camera in URP
            var cameraData = _highlightCamera.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                cameraData.renderType = CameraRenderType.Overlay;
            }

            // Set culling mask to only highlight layer
            if (_highlightLayer != -1)
            {
                _highlightCamera.cullingMask = 1 << _highlightLayer;
            }
        }

        protected virtual void OnTutorialStepChanged(int level, int stepIndex)
        {
            // Clear highlights when step changes (before entry delay)
            ClearHighlights();
        }

        protected virtual void OnTutorialStepEntered(int level, int stepIndex, string stepId)
        {
            // Activate highlights when step actually enters (after entry delay)
            if (!string.IsNullOrEmpty(stepId) && _configByStepId.TryGetValue(stepId, out var config))
            {
                ActivateHighlight(config);
            }
        }

        protected virtual void ActivateHighlight(HighlightConfig config)
        {
            if (config.HighlightTypeIds == null || config.HighlightTypeIds.Length == 0) return;
            if (_highlightLayer == -1) return;
            if (_highlightableProvider == null)
            {
                Debug.LogWarning("[HighlightManager] No highlightable provider set.");
                return;
            }

            // Find items to highlight
            var itemsToHighlight = new List<IHighlightable>(_highlightableProvider.FindHighlightables(config.HighlightTypeIds));

            if (itemsToHighlight.Count == 0)
            {
                Debug.Log("[HighlightManager] No highlightable items found for this step.");
                return;
            }

            _isHighlightActive = true;

            // Show overlay
            if (_overlay != null)
            {
                _overlay.Show();
            }

            // Store and modify main camera culling mask
            if (_mainCamera != null)
            {
                _originalMainCameraCullingMask = _mainCamera.cullingMask;
                // Exclude highlight layer from main camera
                _mainCamera.cullingMask &= ~(1 << _highlightLayer);
            }

            // Enable highlight camera and add to main camera stack
            if (_highlightCamera != null)
            {
                _highlightCamera.gameObject.SetActive(true);

                // Add to camera stack
                if (_mainCamera != null)
                {
                    var mainCameraData = _mainCamera.GetUniversalAdditionalCameraData();
                    if (mainCameraData != null && !mainCameraData.cameraStack.Contains(_highlightCamera))
                    {
                        mainCameraData.cameraStack.Add(_highlightCamera);
                    }
                }
            }

            // Move items to highlight layer and enable visual effects
            foreach (var item in itemsToHighlight)
            {
                // Store original layers for all objects in hierarchy
                _originalLayers[item] = CollectLayers(item.GameObject);

                // Move to highlight layer (including children)
                SetLayerRecursively(item.GameObject, _highlightLayer);

                // Enable visual highlights
                item.SetHighlighted(true);

                // Allow subclasses to add extra visual effects
                OnItemHighlighted(item);

                _highlightedItems.Add(item);
            }

            // Set input layer override
            if (_inputLayerOverride != null)
            {
                _inputLayerOverride.LayerMaskOverride = 1 << _highlightLayer;
            }
        }

        protected virtual void ClearHighlights()
        {
            if (!_isHighlightActive) return;

            // Restore items to original layers
            foreach (var item in _highlightedItems)
            {
                // Check if the underlying Unity object has been destroyed
                if (item is not UnityEngine.Object unityObj || unityObj == null) continue;

                // Restore original layers per-object
                if (_originalLayers.TryGetValue(item, out var layers))
                {
                    RestoreLayers(layers);
                }

                // Disable visual highlights
                item.SetHighlighted(false);

                // Allow subclasses to remove extra visual effects
                OnItemUnhighlighted(item);
            }

            _highlightedItems.Clear();
            _originalLayers.Clear();

            // Restore main camera culling mask
            if (_mainCamera != null)
            {
                _mainCamera.cullingMask = _originalMainCameraCullingMask;
            }

            // Disable highlight camera and remove from stack
            if (_highlightCamera != null)
            {
                _highlightCamera.gameObject.SetActive(false);

                if (_mainCamera != null)
                {
                    var mainCameraData = _mainCamera.GetUniversalAdditionalCameraData();
                    if (mainCameraData != null)
                    {
                        mainCameraData.cameraStack.Remove(_highlightCamera);
                    }
                }
            }

            // Clear input layer override
            if (_inputLayerOverride != null)
            {
                _inputLayerOverride.LayerMaskOverride = 0;
            }

            // Hide overlay
            if (_overlay != null)
            {
                _overlay.Hide();
            }

            _isHighlightActive = false;
        }

        /// <summary>
        /// Override in subclass to add extra visual effects when item is highlighted (e.g., outline).
        /// </summary>
        protected virtual void OnItemHighlighted(IHighlightable item) { }

        /// <summary>
        /// Override in subclass to remove extra visual effects when item is unhighlighted.
        /// </summary>
        protected virtual void OnItemUnhighlighted(IHighlightable item) { }

        protected List<(GameObject go, int layer)> CollectLayers(GameObject root)
        {
            var result = new List<(GameObject, int)>();
            CollectLayersRecursive(root, result);
            return result;
        }

        private void CollectLayersRecursive(GameObject obj, List<(GameObject, int)> result)
        {
            result.Add((obj, obj.layer));
            foreach (Transform child in obj.transform)
                CollectLayersRecursive(child.gameObject, result);
        }

        protected void RestoreLayers(List<(GameObject go, int layer)> layers)
        {
            foreach (var (go, layer) in layers)
            {
                if (go != null)
                    go.layer = layer;
            }
        }

        protected void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
