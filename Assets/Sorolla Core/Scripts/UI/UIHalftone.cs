using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.UI
{
    public enum HalftoneOverlayMode
    {
        Off = 0,
        Multiply = 1,
        Screen = 2
    }

    /// <summary>
    /// Per-element halftone overrides for the UI/Halftone shader.
    /// Attach to any Graphic (Image, RawImage, etc.) to customise
    /// colors, density, angle, etc. without needing a separate material.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public class UIHalftone : MonoBehaviour
    {
        [Header("Halftone Settings")]
        [SerializeField] private Color _colorA = Color.white;
        [SerializeField] private Color _colorB = Color.black;
        [SerializeField, Range(5f, 300f)] private float _density = 40f;
        [SerializeField, Range(0f, 360f)] private float _angle = 45f;
        [SerializeField, Range(0f, 0.5f)] private float _softness = 0.05f;

        [Header("Gradient Direction")]
        [SerializeField, Range(0f, 360f)] private float _gradientAngle;
        [SerializeField, Range(-1f, 1f)] private float _gradientOffset;
        [SerializeField, Range(0.1f, 5f)] private float _gradientScale = 1f;

        [Header("Overlay Image")]
        [SerializeField] private Texture2D _overlayTexture;
        [SerializeField] private Color _overlayTint = Color.white;
        [SerializeField] private HalftoneOverlayMode _overlayMode;

        [Header("Noise")]
        [SerializeField, Range(0.1f, 20f)] private float _noiseScale = 5f;
        [SerializeField, Range(0f, 1f)] private float _noiseStrength;
        [SerializeField, Range(0f, 10f)] private float _noiseAnimSpeed;
        [SerializeField, Range(-5f, 5f)] private float _noiseScrollX;
        [SerializeField, Range(-5f, 5f)] private float _noiseScrollY;

        [Header("Animation")]
        [SerializeField, Range(0f, 10f)] private float _animSpeed;
        [SerializeField, Range(-1f, 1f)] private float _animScrollX = 1f;
        [SerializeField, Range(-1f, 1f)] private float _animScrollY;

        [Header("Shader Reference")]
        [SerializeField] private Shader _halftoneShader;

        private Material _material;
        private Graphic _graphic;

        private static readonly int ColorAProperty = Shader.PropertyToID("_ColorA");
        private static readonly int ColorBProperty = Shader.PropertyToID("_ColorB");
        private static readonly int DensityProperty = Shader.PropertyToID("_Density");
        private static readonly int AngleProperty = Shader.PropertyToID("_Angle");
        private static readonly int SoftnessProperty = Shader.PropertyToID("_Softness");
        private static readonly int GradientAngleProperty = Shader.PropertyToID("_GradientAngle");
        private static readonly int GradientOffsetProperty = Shader.PropertyToID("_GradientOffset");
        private static readonly int GradientScaleProperty = Shader.PropertyToID("_GradientScale");
        private static readonly int OverlayTexProperty = Shader.PropertyToID("_OverlayTex");
        private static readonly int OverlayTintProperty = Shader.PropertyToID("_OverlayTint");
        private static readonly int OverlayModeProperty = Shader.PropertyToID("_OverlayMode");
        private static readonly int NoiseScaleProperty = Shader.PropertyToID("_NoiseScale");
        private static readonly int NoiseStrengthProperty = Shader.PropertyToID("_NoiseStrength");
        private static readonly int NoiseAnimSpeedProperty = Shader.PropertyToID("_NoiseAnimSpeed");
        private static readonly int NoiseScrollXProperty = Shader.PropertyToID("_NoiseScrollX");
        private static readonly int NoiseScrollYProperty = Shader.PropertyToID("_NoiseScrollY");
        private static readonly int AnimSpeedProperty = Shader.PropertyToID("_AnimSpeed");
        private static readonly int AnimScrollXProperty = Shader.PropertyToID("_AnimScrollX");
        private static readonly int AnimScrollYProperty = Shader.PropertyToID("_AnimScrollY");

        private void OnEnable()
        {
            _graphic = GetComponent<Graphic>();
            Initialize();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_halftoneShader == null)
            {
                _halftoneShader = Shader.Find("UI/Halftone");
                if (_halftoneShader != null)
                    UnityEditor.EditorUtility.SetDirty(this);
            }

            if (_material != null)
                ApplyProperties();
        }

        private void Reset()
        {
            _halftoneShader = Shader.Find("UI/Halftone");
        }
#endif

        private void Initialize()
        {
            var shader = _halftoneShader != null ? _halftoneShader : Shader.Find("UI/Halftone");
            if (shader == null)
            {
                Debug.LogError("[UIHalftone] UI/Halftone shader not found! Assign the shader reference in the inspector.");
                return;
            }

            _material = new Material(shader);
            _graphic.material = _material;
            ApplyProperties();
        }

        private void Cleanup()
        {
            if (_material != null)
            {
                if (_graphic != null)
                    _graphic.material = null;

                if (Application.isPlaying)
                    Destroy(_material);
                else
                    DestroyImmediate(_material);
                _material = null;
            }
        }

        private void ApplyProperties()
        {
            _material.SetColor(ColorAProperty, _colorA);
            _material.SetColor(ColorBProperty, _colorB);
            _material.SetFloat(DensityProperty, _density);
            _material.SetFloat(AngleProperty, _angle);
            _material.SetFloat(SoftnessProperty, _softness);
            _material.SetFloat(GradientAngleProperty, _gradientAngle);
            _material.SetFloat(GradientOffsetProperty, _gradientOffset);
            _material.SetFloat(GradientScaleProperty, _gradientScale);

            if (_overlayTexture != null)
                _material.SetTexture(OverlayTexProperty, _overlayTexture);
            _material.SetColor(OverlayTintProperty, _overlayTint);
            _material.SetFloat(OverlayModeProperty, (float)_overlayMode);

            _material.SetFloat(NoiseScaleProperty, _noiseScale);
            _material.SetFloat(NoiseStrengthProperty, _noiseStrength);
            _material.SetFloat(NoiseAnimSpeedProperty, _noiseAnimSpeed);
            _material.SetFloat(NoiseScrollXProperty, _noiseScrollX);
            _material.SetFloat(NoiseScrollYProperty, _noiseScrollY);

            _material.SetFloat(AnimSpeedProperty, _animSpeed);
            _material.SetFloat(AnimScrollXProperty, _animScrollX);
            _material.SetFloat(AnimScrollYProperty, _animScrollY);
        }
    }
}
