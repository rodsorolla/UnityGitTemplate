using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Attach to a Light to create a flickering/sparking effect.
    /// Randomly varies intensity and optionally color to simulate electrical sparks.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        [Header("Intensity")]
        [SerializeField] private float _baseIntensity = 1f;
        [SerializeField, Min(0f)] private float _flickerAmount = 0.5f;
        [SerializeField, Min(0.01f)] private float _flickerSpeed = 15f;

        [Header("Sparks")]
        [Tooltip("Chance per second of a bright spark burst")]
        [SerializeField, Range(0f, 10f)] private float _sparkRate = 2f;
        [SerializeField, Min(1f)] private float _sparkMultiplier = 3f;
        [SerializeField, Min(0.01f)] private float _sparkDecay = 12f;

        [Header("Color (Optional)")]
        [SerializeField] private bool _flickerColor;
        [SerializeField] private Color _baseColor = Color.white;
        [SerializeField] private Color _sparkColor = new Color(1f, 0.85f, 0.5f);

        private Light _light;
        private float _noiseOffset;
        private float _sparkBoost;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _noiseOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Perlin noise for smooth base flicker
            float noise = Mathf.PerlinNoise(_noiseOffset + Time.time * _flickerSpeed, 0f);
            // Remap from [0,1] to [-1,1]
            float flicker = (noise * 2f - 1f) * _flickerAmount;

            // Random spark bursts
            if (_sparkRate > 0f && Random.value < _sparkRate * dt)
                _sparkBoost = _sparkMultiplier;

            _sparkBoost = Mathf.MoveTowards(_sparkBoost, 0f, _sparkDecay * dt);

            _light.intensity = Mathf.Max(0f, _baseIntensity + flicker + _sparkBoost);

            if (_flickerColor)
            {
                float t = Mathf.Clamp01(_sparkBoost / _sparkMultiplier);
                _light.color = Color.Lerp(_baseColor, _sparkColor, t);
            }
        }
    }
}
