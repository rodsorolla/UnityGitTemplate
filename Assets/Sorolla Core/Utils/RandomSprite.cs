using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Picks a random sprite from an array and assigns it to the SpriteRenderer on this GameObject.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class RandomSprite : MonoBehaviour
    {
        [SerializeField] private Sprite[] _sprites;

        private void Awake()
        {
            if (_sprites == null || _sprites.Length == 0) return;

            var sr = GetComponent<SpriteRenderer>();
            sr.sprite = _sprites[Random.Range(0, _sprites.Length)];
        }
    }
}
