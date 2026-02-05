using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.UI.Effects
{
    /// <summary>
    /// Manages pooled floating text popups that appear in world space.
    /// Generic floating text system for scores, damage numbers, etc.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingTextManager : MonoBehaviour
    {
        private static FloatingTextManager _instance;

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static FloatingTextManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<FloatingTextManager>();
                }
                return _instance;
            }
        }

        [Header("Prefab")]
        [SerializeField] private FloatingTextPopup _popupPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int _prewarmCount = 10;
        [SerializeField] private Transform _poolContainer;

        [Header("Display Settings")]
        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.5f, 0f);

        private readonly List<FloatingTextPopup> _pool = new();
        private Camera _mainCamera;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            Initialize();
        }

        private void Initialize()
        {
            if (_popupPrefab == null)
            {
                Debug.LogWarning("[FloatingTextManager] Popup prefab is not assigned.");
                return;
            }

            // Create container if not assigned
            if (_poolContainer == null)
            {
                var containerGO = new GameObject("FloatingTextPool");
                containerGO.transform.SetParent(transform);
                _poolContainer = containerGO.transform;
            }

            // Prewarm pool
            for (int i = 0; i < _prewarmCount; i++)
            {
                CreatePooledInstance();
            }

            _mainCamera = Camera.main;
        }

        private FloatingTextPopup CreatePooledInstance()
        {
            var go = Instantiate(_popupPrefab.gameObject, _poolContainer);
            go.SetActive(false);
            var popup = go.GetComponent<FloatingTextPopup>();
            _pool.Add(popup);
            return popup;
        }

        private FloatingTextPopup GetPooledInstance()
        {
            foreach (var popup in _pool)
            {
                if (popup != null && !popup.gameObject.activeSelf)
                {
                    return popup;
                }
            }

            // Create new instance if pool exhausted
            return CreatePooledInstance();
        }

        /// <summary>
        /// Show floating text at the specified world position.
        /// </summary>
        /// <param name="text">The text to display</param>
        /// <param name="worldPosition">World position where the popup should appear</param>
        /// <param name="color">Optional color (uses default if null)</param>
        /// <param name="scale">Scale multiplier</param>
        public void ShowText(string text, Vector3 worldPosition, Color? color = null, float scale = 1f)
        {
            if (_popupPrefab == null) return;

            var popup = GetPooledInstance();
            if (popup == null) return;

            Vector3 spawnPos = worldPosition + _spawnOffset * scale;

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            popup.Play(text, spawnPos, color ?? _defaultColor, _mainCamera, scale);
        }

        /// <summary>
        /// Show a formatted number with optional prefix.
        /// </summary>
        /// <param name="value">The number to display</param>
        /// <param name="worldPosition">World position</param>
        /// <param name="format">String format (default: "+{0}")</param>
        /// <param name="color">Optional color</param>
        /// <param name="scale">Scale multiplier</param>
        public void ShowNumber(int value, Vector3 worldPosition, string format = "+{0}", Color? color = null, float scale = 1f)
        {
            ShowText(string.Format(format, value), worldPosition, color, scale);
        }

        /// <summary>
        /// Return all active popups to the pool.
        /// </summary>
        public void ReturnAllToPool()
        {
            foreach (var popup in _pool)
            {
                if (popup != null && popup.gameObject.activeSelf)
                {
                    popup.Stop();
                    popup.gameObject.SetActive(false);
                }
            }
        }
    }
}
