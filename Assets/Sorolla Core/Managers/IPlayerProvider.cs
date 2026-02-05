using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Abstraction for player-related data access.
    /// Allows Sorolla Core components to access player without knowing game-specific implementation.
    /// </summary>
    public interface IPlayerProvider
    {
        /// <summary>
        /// Get the current player's transform (for camera follow, tutorial arrows, etc).
        /// </summary>
        Transform GetPlayerTransform();
        
        /// <summary>
        /// Check if a player exists in the current level.
        /// </summary>
        bool HasPlayer();
    }
}

