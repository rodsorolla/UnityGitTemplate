using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Extension methods for common component validation patterns.
    /// Reduces repetitive GetComponent null-checking boilerplate code.
    /// </summary>
    public static class ComponentValidationExtensions
    {
        /// <summary>
        /// Gets or assigns a component of type T.
        /// If the reference is null, attempts to GetComponent and assigns it.
        /// </summary>
        public static void GetOrAssign<T>(this MonoBehaviour mb, ref T component) where T : Component
        {
            if (component == null)
            {
                component = mb.GetComponent<T>();
            }
        }
    }
}

