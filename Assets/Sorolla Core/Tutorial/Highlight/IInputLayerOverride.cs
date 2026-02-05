using UnityEngine;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Interface for input systems that support layer mask overriding.
    /// Used by highlight system to restrict input to highlighted objects.
    /// </summary>
    public interface IInputLayerOverride
    {
        /// <summary>
        /// Layer mask override for raycasting. When non-zero, only these layers are raycasted.
        /// Set to 0 to clear the override and restore default behavior.
        /// </summary>
        LayerMask LayerMaskOverride { get; set; }
    }
}
