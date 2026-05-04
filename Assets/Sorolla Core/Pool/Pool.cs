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
        // Queue of inactive members, maintained O(1) by PoolMember.OnDisable.
        // A stale or externally re-activated entry is skipped on dequeue.
        private readonly Queue<PoolMember> _freeMembers = new();
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

            // Fast path: dequeue from the inactive queue. Skips destroyed objects
            // and false positives where something re-enabled the object externally.
            while (_freeMembers.Count > 0)
            {
                var member = _freeMembers.Dequeue();
                if (member == null) continue;
                member.IsQueued = false;
                if (member.gameObject.activeSelf) continue;
                member.gameObject.SetActive(true);
                return member.gameObject;
            }

            // No inactive members available — grow the pool.
            return CreateObject(true);
        }

        /// <summary>
        /// Called by <see cref="PoolMember"/> when a pooled object's OnDisable fires.
        /// Keeps the inactive queue in sync without requiring callers to use an
        /// explicit "return to pool" API — SetActive(false) is enough.
        /// </summary>
        internal void OnMemberDisabled(PoolMember member)
        {
            if (!_initialized || member == null) return;
            _freeMembers.Enqueue(member);
            member.IsQueued = true;
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
            _freeMembers.Clear();
        }

        private GameObject CreateObject(bool active)
        {
            var obj = Object.Instantiate(_prefab, _container);
            obj.name = $"{_name} #{_pooledObjects.Count}";
            obj.SetActive(active);
            _pooledObjects.Add(obj);

            // Attach the return-tracking component. Instantiating inactive does not
            // fire OnDisable, so prewarmed objects are enqueued manually here.
            var member = obj.AddComponent<PoolMember>();
            member.Pool = this;
            if (!active)
            {
                _freeMembers.Enqueue(member);
                member.IsQueued = true;
            }

            return obj;
        }
    }

    /// <summary>
    /// Per-instance tracker added to every pooled object so the owning <see cref="Pool"/>
    /// can maintain an O(1) free queue. Enqueues itself on OnDisable, with a guard
    /// against double-enqueue if callers toggle active state repeatedly.
    /// </summary>
    internal sealed class PoolMember : MonoBehaviour
    {
        internal Pool Pool;
        internal bool IsQueued;

        private void OnDisable()
        {
            if (IsQueued || Pool == null) return;
            Pool.OnMemberDisabled(this);
        }
    }
}
