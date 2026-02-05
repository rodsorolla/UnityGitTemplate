using System.Collections.Generic;

namespace Sorolla.Tutorial.Highlight
{
    /// <summary>
    /// Interface for finding highlightable objects in the scene.
    /// Implement this in game-specific code to provide highlightables.
    /// </summary>
    public interface IHighlightableProvider
    {
        /// <summary>
        /// Find all highlightable objects matching the given type IDs.
        /// </summary>
        /// <param name="typeIds">Array of type IDs to match against IHighlightable.HighlightTypeId</param>
        /// <returns>Collection of matching highlightables that can currently be highlighted</returns>
        IEnumerable<IHighlightable> FindHighlightables(string[] typeIds);
    }
}
