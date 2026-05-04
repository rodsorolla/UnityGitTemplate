using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.UI
{
    /// <summary>
    /// Applies a gradient to a UI Image using Unity's Gradient editor.
    /// Requires the UI/Gradient shader.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public class UIGradient : BaseMeshEffect
    {
        [SerializeField] private Gradient _gradient = new Gradient();
        [SerializeField, Range(0f, 360f)] private float _angle = 0f;
        [SerializeField, Range(16, 256)] private int _resolution = 64;

        [Header("Shader Reference")]
        [SerializeField] private Shader _gradientShader;

        private Material _material;
        private Texture2D _gradientTexture;

        private static readonly int GradientTexProperty = Shader.PropertyToID("_GradientTex");
        private static readonly int AngleProperty = Shader.PropertyToID("_Angle");

        public Gradient Gradient
        {
            get => _gradient;
            set
            {
                _gradient = value;
                UpdateGradientTexture();
                graphic.SetVerticesDirty();
            }
        }

        public float Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                if (_material != null)
                    _material.SetFloat(AngleProperty, _angle);
                graphic.SetVerticesDirty();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Initialize();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Cleanup();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Cleanup();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            // Auto-assign shader reference if missing
            if (_gradientShader == null)
            {
                _gradientShader = Shader.Find("UI/Gradient");
                if (_gradientShader != null)
                    UnityEditor.EditorUtility.SetDirty(this);
            }

            if (_material != null)
            {
                UpdateGradientTexture();
                _material.SetFloat(AngleProperty, _angle);
            }

            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        protected override void Reset()
        {
            base.Reset();
            _gradientShader = Shader.Find("UI/Gradient");
        }
#endif

        private void Initialize()
        {
            // Use serialized reference first (survives build stripping), fallback to Shader.Find for editor convenience
            var shader = _gradientShader != null ? _gradientShader : Shader.Find("UI/Gradient");
            if (shader == null)
            {
                Debug.LogError("[UIGradient] UI/Gradient shader not found! Assign the shader reference in the inspector.");
                return;
            }

            _material = new Material(shader);
            graphic.material = _material;

            UpdateGradientTexture();
            _material.SetFloat(AngleProperty, _angle);
        }

        private void Cleanup()
        {
            if (_gradientTexture != null)
            {
                if (Application.isPlaying)
                    Destroy(_gradientTexture);
                else
                    DestroyImmediate(_gradientTexture);
                _gradientTexture = null;
            }

            if (_material != null)
            {
                if (graphic != null)
                    graphic.material = null;

                if (Application.isPlaying)
                    Destroy(_material);
                else
                    DestroyImmediate(_material);
                _material = null;
            }
        }

        private void UpdateGradientTexture()
        {
            if (_material == null) return;

            if (_gradientTexture == null || _gradientTexture.width != _resolution)
            {
                if (_gradientTexture != null)
                {
                    if (Application.isPlaying)
                        Destroy(_gradientTexture);
                    else
                        DestroyImmediate(_gradientTexture);
                }

                _gradientTexture = new Texture2D(_resolution, 1, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            var colors = new Color[_resolution];
            for (int i = 0; i < _resolution; i++)
            {
                float t = i / (float)(_resolution - 1);
                colors[i] = _gradient.Evaluate(t);
            }

            _gradientTexture.SetPixels(colors);
            _gradientTexture.Apply();

            _material.SetTexture(GradientTexProperty, _gradientTexture);
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
                return;

            // Get all vertices
            var vertices = new List<UIVertex>();
            vh.GetUIVertexStream(vertices);

            // Find bounds of the mesh
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            for (int i = 0; i < vertices.Count; i++)
            {
                var pos = vertices[i].position;
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
            }

            float width = maxX - minX;
            float height = maxY - minY;

            if (width <= 0 || height <= 0)
                return;

            // Store normalized position in UV1
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                float normalizedX = (vertex.position.x - minX) / width;
                float normalizedY = (vertex.position.y - minY) / height;
                vertex.uv1 = new Vector4(normalizedX, normalizedY, 0, 0);
                vertices[i] = vertex;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(vertices);
        }
    }
}
