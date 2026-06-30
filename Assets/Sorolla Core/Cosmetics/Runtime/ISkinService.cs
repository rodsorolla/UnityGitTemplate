using System;

namespace Sorolla.Cosmetics
{
    /// <summary>Tracks which skins are unlocked and which is selected. Persistence-backed.</summary>
    public interface ISkinService
    {
        bool IsUnlocked(string id);
        void Unlock(string id);
        string SelectedSkinId { get; }
        bool Select(string id);
        event Action OnChanged;
    }
}
