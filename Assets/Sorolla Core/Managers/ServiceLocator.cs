using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Simple service locator implementation for dependency injection.
    /// Replaces direct singleton access patterns throughout Sorolla Core.
    /// </summary>
    public class ServiceLocator : IServiceProvider
    {
        private static ServiceLocator _instance;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServiceLocator();
                }
                return _instance;
            }
        }

        public void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} is already registered. Overwriting.");
            }
            _services[type] = service;
        }

        public T Resolve<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }
            Debug.LogError($"[ServiceLocator] Service {type.Name} not found.");
            return null;
        }

        public T TryResolve<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }
            return null;
        }

        public bool Has<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        public void Clear()
        {
            _services.Clear();
        }

        /// <summary>
        /// Reset the singleton instance (useful for tests).
        /// </summary>
        public static void Reset()
        {
            _instance?.Clear();
            _instance = null;
        }
    }
}

