using System;

namespace Sorolla.Cosmetics
{
    /// <summary>Tracks which skins are unlocked and which is selected. Persistence-backed.</summary>
    public interface ISkinService
    {
        bool IsUnlocked(string id);
        void Unlock(string id);
        /// <summary>Removes ownership of a skin. No-op for default-unlocked skins (always owned).</summary>
        void Relock(string id);
        string SelectedSkinId { get; }
        bool Select(string id);
        event Action OnChanged;
    }
}
