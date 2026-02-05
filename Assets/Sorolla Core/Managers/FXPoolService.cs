using System.Collections.Generic;
using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Centralized service managing FX/object pools.
    /// </summary>
    [DisallowMultipleComponent]
    public class FXPoolService : MonoSingleton<FXPoolService>
    {
        private readonly Dictionary<string, Pool> _pools = new();

        /// <summary>
        /// Ensure a pool exists for the given id. If missing, creates one for the prefab.
        /// </summary>
        public Pool EnsurePool(string id, GameObject prefab, int prewarm = 0, Transform parent = null)
        {
            if (string.IsNullOrEmpty(id)) id = prefab != null ? prefab.name : "Pool";

            if (_pools.TryGetValue(id, out var pool) && pool != null)
                return pool;

            if (PoolManager.HasPool(id))
            {
                pool = PoolManager.GetPoolByName(id) as Pool;
            }
            else
            {
                if (prefab == null)
                {
                    Debug.LogError($"[FXPoolService] Can't create pool '{id}' because prefab is null.");
                    return null;
                }
                pool = parent != null ? new Pool(prefab, id, parent) : new Pool(prefab, id);
                if (prewarm > 0)
                {
                    pool.CreatePoolObjects(prewarm);
                }
            }

            _pools[id] = pool;
            return pool;
        }

        public bool HasPool(string id)
        {
            return _pools.ContainsKey(id) || PoolManager.HasPool(id);
        }

        public Pool GetPool(string id)
        {
            if (_pools.TryGetValue(id, out var pool) && pool != null)
                return pool;
            if (PoolManager.HasPool(id))
            {
                pool = PoolManager.GetPoolByName(id) as Pool;
                if (pool != null) _pools[id] = pool;
                return pool;
            }
            return null;
        }

        public T GetPooledComponent<T>(string id) where T : Component
        {
            var pool = GetPool(id);
            return pool != null ? pool.GetPooledComponent<T>() : null;
        }

        public GameObject GetPooledObject(string id)
        {
            var pool = GetPool(id);
            return pool != null ? pool.GetPooledObject() : null;
        }
    }
}

