using System.Threading.Tasks;
using UnityEngine;

namespace Sorolla.UI
{
    /// <summary>
    /// Interface for UI transition animations.
    /// Implement this to create custom panel/screen transitions.
    /// </summary>
    public interface IUITransition
    {
        /// <summary>
        /// Play the enter/show transition animation.
        /// </summary>
        /// <param name="target">The transform to animate</param>
        /// <returns>Task that completes when animation finishes</returns>
        Task PlayEnterAsync(Transform target);

        /// <summary>
        /// Play the exit/hide transition animation.
        /// </summary>
        /// <param name="target">The transform to animate</param>
        /// <returns>Task that completes when animation finishes</returns>
        Task PlayExitAsync(Transform target);
    }
}
