using UnityEngine;

namespace Sorolla
{
    public class InfinityMovement : MonoBehaviour
    {
        [SerializeField] private float _speed = 2f;
        [SerializeField] private float _scale = 5f;
        [SerializeField] private bool _ignoreTimeScale;

        private float time = 0f;

        private void Update()
        {
            time += _ignoreTimeScale ? Time.unscaledDeltaTime * _speed : Time.deltaTime * _speed;

            // Parametric equations for a figure-8 (lemniscate of Gerono)
            float x = Mathf.Sin(time) * _scale * 100f;
            float y = Mathf.Sin(time) * Mathf.Cos(time) * _scale * 100f;

            transform.localPosition = new Vector3(x, y, transform.localPosition.z);
        }
    }
}