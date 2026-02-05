using UnityEngine;

namespace Sorolla.Tutorial
{
    /// <summary>
    /// Follows player with an offset. Resolves player from ServiceLocator.
    /// </summary>
    public class FollowPlayerWithOffset : MonoBehaviour
    {
        [SerializeField] private Vector3 _offset;
        
        private Transform _playerTransform;
        private IPlayerProvider _playerProvider;

        private void Start()
        {
            // Cache the provider for repeated access
            _playerProvider = ServiceLocator.Instance.TryResolve<IPlayerProvider>();
        }

        public void SetTarget(Transform target) => _playerTransform = target;

        private void LateUpdate()
        {
            // If we don't have a player transform yet, try to get it from the provider
            if (_playerTransform == null && _playerProvider != null)
            {
                _playerTransform = _playerProvider.GetPlayerTransform();
            }
            
            if (_playerTransform == null) return;
            transform.position = _playerTransform.position + _offset;
        }
    }
}