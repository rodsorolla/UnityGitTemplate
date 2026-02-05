using UnityEngine;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Interface for objects that can be highlighted during tutorials.
    /// </summary>
    public interface IHighlightable
    {
        /// <summary>
        /// Identifier for the type of highlightable (used to match with config).
        /// </summary>
        string HighlightTypeId { get; }

        /// <summary>
        /// Whether the object is currently available to be highlighted.
        /// </summary>
        bool CanBeHighlighted { get; }

        /// <summary>
        /// The GameObject this highlightable is attached to.
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// Set the highlighted visual state.
        /// </summary>
        void SetHighlighted(bool highlighted);
    }
}
