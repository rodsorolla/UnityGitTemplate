﻿using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Base class for Sorolla managers with simple initialization pattern.
    /// Override Initialize() for setup logic. Init() is idempotent.
    /// </summary>
    public abstract class SorollaManager : MonoBehaviour
    {
        private bool _initialized;
        
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Initialize the manager. Idempotent - safe to call multiple times.
        /// </summary>
        public void Init()
        {
            if (_initialized) return;
            Initialize();
            PostInitialize();
            _initialized = true;
        }

        /// <summary>
        /// Override to implement initialization logic.
        /// </summary>
        protected virtual void Initialize() { }
        
        /// <summary>
        /// Optional post-initialize hook.
        /// </summary>
        protected virtual void PostInitialize() { }

        /// <summary>
        /// Reset initialization state if re-initialization is needed.
        /// </summary>
        public virtual void Teardown() => _initialized = false;
    }
}
