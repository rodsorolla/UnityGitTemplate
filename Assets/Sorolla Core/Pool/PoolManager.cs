using System.Collections.Generic;
using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Static manager that tracks all registered pools.
    /// </summary>
    public static class PoolManager
    {
        private static readonly Dictionary<string, Pool> _pools = new();

        /// <summary>
        /// Registers a pool with the manager.
        /// </summary>
        public static void Register(Pool pool)
        {
            if (pool == null || string.IsNullOrEmpty(pool.Name)) return;

            if (_pools.ContainsKey(pool.Name))
            {
                Debug.LogWarning($"[PoolManager] Pool '{pool.Name}' already registered.");
                return;
            }

            _pools[pool.Name] = pool;
        }

        /// <summary>
        /// Checks if a pool with the given name exists.
        /// </summary>
        public static bool HasPool(string name)
        {
            return !string.IsNullOrEmpty(name) && _pools.ContainsKey(name);
        }

        /// <summary>
        /// Gets a pool by name, or null if not found.
        /// </summary>
        public static Pool GetPoolByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            _pools.TryGetValue(name, out var pool);
            return pool;
        }

        /// <summary>
        /// Returns all objects to their pools.
        /// </summary>
        public static void ReturnAllToPool()
        {
            foreach (var pool in _pools.Values)
            {
                pool?.ReturnToPoolEverything(true);
            }
        }

        /// <summary>
        /// Clears all pools and removes them from the manager.
        /// </summary>
        public static void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool?.Clear();
            }
            _pools.Clear();
        }

        /// <summary>
        /// Removes a pool from the manager.
        /// </summary>
        public static void Unregister(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

            if (_pools.TryGetValue(name, out var pool))
            {
                pool?.Clear();
                _pools.Remove(name);
            }
        }
    }
}
