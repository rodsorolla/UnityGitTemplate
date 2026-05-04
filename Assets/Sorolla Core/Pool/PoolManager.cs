using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sorolla
{
    /// <summary>
    /// Static manager that tracks all registered pools.
    /// </summary>
    public static class PoolManager
    {
        private static readonly Dictionary<string, Pool> _pools = new();
        private static bool _sceneHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _pools.Clear();
            _sceneHooked = false;
        }

        private static void EnsureSceneHook()
        {
            if (_sceneHooked) return;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            _sceneHooked = true;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            // Remove pools whose prefab was destroyed with the unloaded scene
            var stale = new List<string>();
            foreach (var kvp in _pools)
            {
                if (kvp.Value == null || kvp.Value.Prefab == null)
                    stale.Add(kvp.Key);
            }
            foreach (var key in stale)
                _pools.Remove(key);
        }

        /// <summary>
        /// Registers a pool with the manager.
        /// </summary>
        public static void Register(Pool pool)
        {
            if (pool == null || string.IsNullOrEmpty(pool.Name)) return;
            EnsureSceneHook();

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
