using Sorolla.PersistentData;

namespace Sorolla.Profile
{
    [System.Serializable]
    public class PlayerProfileData : ISaveData
    {
        // ISaveData — constant; reserved for future migrations.
        public int Version => 1;

        public string PlayerId;        // immutable per-install id; identity seam for Tournaments
        public string DisplayName;
        public string AvatarId;
        public string CountryCode;
        public bool IsCustomName;
    }
}
