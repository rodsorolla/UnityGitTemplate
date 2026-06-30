using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.UI.Effects
{
    /// <summary>
    /// Hidden worker that bakes a desaturation amount into its graphic's TEXCOORD1 channel
    /// for the UI/Saturation shader. One lives on every Image driven by a <see cref="UIImageSaturation"/>
    /// controller; the controller creates, updates, and destroys these automatically.
    ///
    /// Not meant to be added by hand — it is hidden from the Add Component menu and the inspector,
    /// and is never saved to a scene or prefab (HideFlags.HideAndDontSave).
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class UISaturationVertexModifier : BaseMeshEffect
    {
        private float _desaturation;
        private Material _originalMaterial;
        private bool _materialApplied;

        /// <summary>0 = full colour, 1 = fully greyscale.</summary>
        public float Desaturation
        {
            get => _desaturation;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(_desaturation, value)) return;
                _desaturation = value;
                if (graphic != null) graphic.SetVerticesDirty();
            }
        }

        /// <summary>
        /// Swap the graphic onto the UI/Saturation material so the baked uv1 is actually read,
        /// remembering the original so it can be restored when this worker is removed.
        /// </summary>
        public void AssignMaterial(Material material)
        {
            if (graphic == null || material == null || graphic.material == material) return;
            if (!_materialApplied)
            {
                _originalMaterial = graphic.material;
                _materialApplied = true;
            }
            graphic.material = material;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureCanvasChannel();
            if (graphic != null) graphic.SetVerticesDirty();
        }

        /// <summary>Put the original material back. Safe to call repeatedly; a no-op after the first restore.</summary>
        public void RestoreMaterial()
        {
            if (_materialApplied && graphic != null) graphic.material = _originalMaterial;
            _materialApplied = false;
            _originalMaterial = null;
        }

        protected override void OnDestroy()
        {
            RestoreMaterial();
            base.OnDestroy();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            Vector4 uv1 = new Vector4(_desaturation, 0f, 0f, 0f);
            UIVertex vertex = default;
            int count = vh.currentVertCount;
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                vertex.uv1 = uv1;
                vh.SetUIVertex(vertex, i);
            }
        }

        /// <summary>
        /// The Canvas only forwards TEXCOORD1 to the shader when this channel is enabled,
        /// so opt in here to guarantee the baked saturation actually reaches the GPU.
        /// </summary>
        private void EnsureCanvasChannel()
        {
            Canvas c = graphic != null ? graphic.canvas : null;
            if (c != null)
                c.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        }
    }
}
