using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.UI.Effects
{
    /// <summary>
    /// Per-image saturation control for UI graphics. 1 = full color, 0 = fully greyscale.
    /// Requires the UI/Saturation shader on the affected graphics' material.
    ///
    /// Place it on a single Image to control just that image, or on a parent (Image or not)
    /// with <see cref="_includeChildren"/> enabled to desaturate the whole UI subtree.
    ///
    /// The saturation is baked into each graphic's TEXCOORD1 channel rather than written to the
    /// material, so any number of images can share ONE material yet each render at its own
    /// saturation while still batching. Because a mesh effect can only touch the graphic on its
    /// own GameObject, this controller attaches a hidden <see cref="UISaturationVertexModifier"/>
    /// to every affected Image and drives them. Those workers are managed automatically and never
    /// saved to the scene/prefab.
    /// </summary>
    [AddComponentMenu("UI/Effects/Image Saturation")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class UIImageSaturation : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)]
        [Tooltip("1 = full color, 0 = fully greyscale.")]
        private float _saturation = 1f;

        [SerializeField]
        [Tooltip("When on, affects this GameObject's Image (if any) plus every descendant Image. When off, only this GameObject's Image.")]
        private bool _includeChildren = true;

        [SerializeField]
        [Tooltip("Also affect Images on inactive child GameObjects so they look correct when enabled.")]
        private bool _includeInactive = true;

        [SerializeField]
        [Tooltip("Material using the UI/Saturation shader. Affected Images on the default UI material are switched to this so the effect is visible, and restored when removed. Auto-filled when the component is added.")]
        private Material _saturationMaterial;

        private readonly List<UISaturationVertexModifier> _managed = new();

        // Reused scratch buffers to avoid per-Apply allocations.
        private static readonly List<Graphic> s_graphics = new();

        public float Saturation
        {
            get => _saturation;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(_saturation, value)) return;
                _saturation = value;
                PushValue();
            }
        }

        /// <summary>Re-scan target graphics and resync helpers. Call after changing the hierarchy at runtime.</summary>
        public void Refresh() => Apply();

        private void OnEnable() => Apply();

        private void OnDisable() => Teardown();

        private void OnTransformChildrenChanged()
        {
            if (_includeChildren && isActiveAndEnabled) ScheduleApply();
        }

        /// <summary>Collect targets, ensure a worker on each, drop workers that are no longer targets.</summary>
        private void Apply()
        {
            CollectTargets(s_graphics);
            float desaturation = 1f - _saturation;

            // Remove workers whose graphic dropped out of the target set.
            for (int i = _managed.Count - 1; i >= 0; i--)
            {
                UISaturationVertexModifier worker = _managed[i];
                if (worker == null || !s_graphics.Contains(worker.GetComponent<Graphic>()))
                {
                    if (worker != null)
                    {
                        worker.Desaturation = 0f; // restore full colour first
                        worker.RestoreMaterial(); // restore synchronously, before the deferred destroy
                    }
                    DestroyWorker(worker);
                    _managed.RemoveAt(i);
                }
            }

            // Ensure a worker on each target graphic.
            foreach (Graphic g in s_graphics)
            {
                UISaturationVertexModifier worker = g.GetComponent<UISaturationVertexModifier>();
                if (worker == null)
                {
                    worker = g.gameObject.AddComponent<UISaturationVertexModifier>();
                    worker.hideFlags = HideFlags.HideAndDontSave;
                }
                if (_saturationMaterial != null && g.material == g.defaultMaterial)
                    worker.AssignMaterial(_saturationMaterial);
                worker.Desaturation = desaturation;
                if (!_managed.Contains(worker)) _managed.Add(worker);
            }

            s_graphics.Clear();
        }

        private void CollectTargets(List<Graphic> results)
        {
            results.Clear();
            if (_includeChildren)
                GetComponentsInChildren(_includeInactive, results);
            else
            {
                Graphic self = GetComponent<Graphic>();
                if (self != null) results.Add(self);
            }

            for (int i = results.Count - 1; i >= 0; i--)
            {
                Graphic g = results[i];
                // Hand off graphics governed by a nearer nested controller to that controller.
                if (_includeChildren && g.GetComponentInParent<UIImageSaturation>(true) != this)
                {
                    results.RemoveAt(i);
                    continue;
                }
                // Skip graphics we can't put on the UI/Saturation shader (e.g. custom-shaded particles).
                if (!IsManageable(g)) results.RemoveAt(i);
            }
        }

        /// <summary>A graphic is manageable if it already uses the UI/Saturation shader, or sits on the default UI material and we have a saturation material to swap in.</summary>
        private bool IsManageable(Graphic g)
        {
            if (g == null) return false;
            Material m = g.material;
            if (m != null && m.shader != null && m.shader.name == "UI/Saturation") return true;
            return _saturationMaterial != null && m == g.defaultMaterial;
        }

        /// <summary>Cheap value-only update for existing workers (slider drag / animation), no structural changes.</summary>
        private void PushValue()
        {
            float desaturation = 1f - _saturation;
            for (int i = _managed.Count - 1; i >= 0; i--)
            {
                UISaturationVertexModifier worker = _managed[i];
                if (worker == null) { _managed.RemoveAt(i); continue; }
                worker.Desaturation = desaturation;
            }
        }

        private void Teardown()
        {
            foreach (UISaturationVertexModifier worker in _managed)
            {
                if (worker == null) continue;
                worker.Desaturation = 0f;   // restore full colour
                worker.RestoreMaterial();   // restore synchronously, before the deferred destroy
                DestroyWorker(worker);
            }
            _managed.Clear();
        }

        private static void DestroyWorker(UISaturationVertexModifier worker)
        {
            if (worker == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (worker != null) Object.DestroyImmediate(worker);
                };
                return;
            }
#endif
            Object.Destroy(worker);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (_saturationMaterial != null) return;
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Material"))
            {
                Material m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (m != null && m.shader != null && m.shader.name == "UI/Saturation")
                {
                    _saturationMaterial = m;
                    break;
                }
            }
        }

        private void OnValidate()
        {
            PushValue();   // instant preview on existing workers (safe inside OnValidate)
            // Structural resync (AddComponent/Destroy) is illegal inside OnValidate. OnValidate also
            // fires in play mode (e.g. on Instantiate), so always defer here — don't gate on isPlaying.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled) Apply();
            };
        }
#endif

        private void ScheduleApply()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && isActiveAndEnabled) Apply();
                };
                return;
            }
#endif
            Apply();
        }
    }
}
