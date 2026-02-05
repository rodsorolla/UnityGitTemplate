using System.Collections.Generic;
using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Simple object pool for efficient GameObject reuse.
    /// </summary>
    [System.Serializable]
    public sealed class Pool
    {
        private readonly GameObject _prefab;
        private readonly Transform _container;
        private readonly string _name;
        private readonly List<GameObject> _pooledObjects = new();
        private bool _initialized;

        public string Name => _name;
        public GameObject Prefab => _prefab;

        public Pool(GameObject prefab, string name = null, Transform container = null)
        {
            _prefab = prefab;
            _name = string.IsNullOrEmpty(name) ? prefab.name : name;
            _container = container;
            Init();
        }

        private void Init()
        {
            if (_initialized) return;
            if (_prefab == null)
            {
                Debug.LogError($"[Pool] Initialization failed - prefab is null for pool: {_name}");
                return;
            }

            PoolManager.Register(this);
            _initialized = true;
        }

        /// <summary>
        /// Pre-creates pool objects for better runtime performance.
        /// </summary>
        public void CreatePoolObjects(int count)
        {
            if (!_initialized) Init();

            int toCreate = count - _pooledObjects.Count;
            for (int i = 0; i < toCreate; i++)
            {
                CreateObject(false);
            }
        }

        /// <summary>
        /// Gets an available pooled object, or creates a new one if needed.
        /// </summary>
        public GameObject GetPooledObject()
        {
            if (!_initialized) Init();

            // Find an inactive object
            for (int i = 0; i < _pooledObjects.Count; i++)
            {
                var obj = _pooledObjects[i];
                if (obj == null)
                {
                    Debug.LogWarning($"[Pool] Object in pool '{_name}' was destroyed externally.");
                    continue;
                }

                if (!obj.activeSelf)
                {
                    obj.SetActive(true);
                    return obj;
                }
            }

            // Create new object
            return CreateObject(true);
        }

        /// <summary>
        /// Gets a pooled object and returns a component of type T.
        /// </summary>
        public T GetPooledComponent<T>() where T : Component
        {
            var obj = GetPooledObject();
            return obj != null ? obj.GetComponent<T>() : null;
        }

        /// <summary>
        /// Returns all active pooled objects back to the pool.
        /// </summary>
        public void ReturnToPoolEverything(bool resetParent = false)
        {
            if (!_initialized) return;

            for (int i = 0; i < _pooledObjects.Count; i++)
            {
                var obj = _pooledObjects[i];
                if (obj == null) continue;

                if (resetParent && _container != null)
                {
                    obj.transform.SetParent(_container);
                }
                obj.SetActive(false);
            }
        }

        /// <summary>
        /// Destroys all pooled objects and clears the pool.
        /// </summary>
        public void Clear()
        {
            if (!_initialized) return;

            for (int i = 0; i < _pooledObjects.Count; i++)
            {
                if (_pooledObjects[i] != null)
                {
                    Object.Destroy(_pooledObjects[i]);
                }
            }
            _pooledObjects.Clear();
        }

        private GameObject CreateObject(bool active)
        {
            var obj = Object.Instantiate(_prefab, _container);
            obj.name = $"{_name} #{_pooledObjects.Count}";
            obj.SetActive(active);
            _pooledObjects.Add(obj);
            return obj;
        }
    }
}
