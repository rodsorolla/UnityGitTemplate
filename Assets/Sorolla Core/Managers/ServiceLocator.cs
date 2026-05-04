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

        /// <summary>
        /// Register a service under its concrete runtime type (non-generic).
        /// Useful when the compile-time type is a base class (e.g. MonoBehaviour).
        /// </summary>
        public void RegisterByConcreteType(object service)
        {
            if (service == null) return;
            var type = service.GetType();
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

        public void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
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

        /// <summary>
        /// Logs all registered services to the console. Only runs in Editor and Development builds.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public void DEBUG_LogAll()
        {
            var sb = new System.Text.StringBuilder("[ServiceLocator] Registered services:\n");
            foreach (var kvp in _services)
                sb.AppendLine($"  {kvp.Key.Name} → {kvp.Value?.GetType().Name}");
            Debug.Log(sb.ToString());
        }
    }
}

