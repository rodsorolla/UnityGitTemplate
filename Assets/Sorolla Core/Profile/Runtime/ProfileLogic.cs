namespace Sorolla.Profile
{
    /// Pure, testable profile rules: first-run seeding and stale-catalog-id repair.
    public static class ProfileLogic
    {
        public static PlayerProfileData SeedDefaults(ProfileCatalog catalog, string deviceCountryCode, int randomNameSuffix)
        {
            string country = (catalog != null && catalog.HasFlag(deviceCountryCode))
                ? deviceCountryCode
                : (catalog != null ? catalog.defaultCountryCode : deviceCountryCode);

            return new PlayerProfileData
            {
                PlayerId = System.Guid.NewGuid().ToString("N"),
                DisplayName = "Player" + randomNameSuffix,
                AvatarId = catalog != null ? catalog.FirstAvatarId : null,
                CountryCode = country,
                IsCustomName = false
            };
        }

        public static string ResolveAvatarId(ProfileCatalog catalog, string savedId)
        {
            if (catalog == null) return savedId;
            return catalog.HasAvatar(savedId) ? savedId : catalog.FirstAvatarId;
        }

        public static string ResolveCountryCode(ProfileCatalog catalog, string savedCode)
        {
            if (catalog == null) return savedCode;
            return catalog.HasFlag(savedCode) ? savedCode : catalog.defaultCountryCode;
        }
    }
}
