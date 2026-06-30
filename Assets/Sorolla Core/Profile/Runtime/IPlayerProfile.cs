using System;

namespace Sorolla.Profile
{
    /// The reusable seam consumed by UI and (later) Tournaments.
    public interface IPlayerProfile
    {
        /// Immutable per-install identifier. Stable across name/avatar changes; safe to key data on.
        string PlayerId { get; }
        string DisplayName { get; }
        string AvatarId { get; }
        string CountryCode { get; }

        /// Validates and, on Ok, persists + raises OnProfileChanged. Returns the validation result.
        NameValidationResult SetName(string name);

        /// No-op if the id isn't in the catalog; otherwise persists + raises OnProfileChanged.
        void SetAvatar(string avatarId);

        /// No-op if the code isn't in the catalog; otherwise persists + raises OnProfileChanged.
        void SetFlag(string countryCode);

        event Action OnProfileChanged;
    }
}
