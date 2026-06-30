using System;
using System.Globalization;
using UnityEngine;
using Sorolla.PersistentData;

namespace Sorolla.Profile
{
    /// Loads/seeds/edits/persists the local player profile and registers IPlayerProfile.
    /// Add this component to a manager GameObject in the Init scene and assign the catalog.
    public class PlayerProfileService : SorollaManager, IPlayerProfile
    {
        private const string SaveFile = "profile";

        [SerializeField] private ProfileCatalog _catalog;

        private PlayerProfileData _data;

        public string PlayerId => _data?.PlayerId;
        public string DisplayName => _data?.DisplayName;
        public string AvatarId => _data?.AvatarId;
        public string CountryCode => _data?.CountryCode;

        public event Action OnProfileChanged;

        protected override void Initialize()
        {
            Load();
            ServiceLocator.Instance.Register<IPlayerProfile>(this);
        }

        private void Load()
        {
            var loaded = SaveSystem.Load<PlayerProfileData>(SaveFile);
            bool isFirstRun = loaded == null || string.IsNullOrEmpty(loaded.DisplayName);

            if (isFirstRun)
            {
                _data = ProfileLogic.SeedDefaults(_catalog, DetectDeviceCountry(), UnityEngine.Random.Range(1000, 10000));
                Save();
                return;
            }

            // Repair stale catalog references without overwriting a chosen name.
            _data = loaded;
            string avatar = ProfileLogic.ResolveAvatarId(_catalog, _data.AvatarId);
            string country = ProfileLogic.ResolveCountryCode(_catalog, _data.CountryCode);
            bool changed = avatar != _data.AvatarId || country != _data.CountryCode;
            _data.AvatarId = avatar;
            _data.CountryCode = country;

            // Backfill an id for installs that predate PlayerId.
            if (string.IsNullOrEmpty(_data.PlayerId))
            {
                _data.PlayerId = Guid.NewGuid().ToString("N");
                changed = true;
            }

            if (changed) Save();    // persist the repair/backfill once so it survives next launch
        }

        public NameValidationResult SetName(string name)
        {
            if (_data == null) return NameValidationResult.Empty;
            var blocklist = _catalog != null ? _catalog.nameBlocklist : null;
            var result = DisplayNameValidator.Validate(name, blocklist);
            if (result != NameValidationResult.Ok) return result;

            _data.DisplayName = name.Trim();
            _data.IsCustomName = true;
            Save();
            OnProfileChanged?.Invoke();
            return NameValidationResult.Ok;
        }

        public void SetAvatar(string avatarId)
        {
            if (_data == null) return;
            if (_catalog != null && !_catalog.HasAvatar(avatarId)) return;
            _data.AvatarId = avatarId;
            Save(createBackup: false);   // cosmetic; skip the per-tap backup copy (iOS disk I/O)
            OnProfileChanged?.Invoke();
        }

        public void SetFlag(string countryCode)
        {
            if (_data == null) return;
            if (_catalog != null && !_catalog.HasFlag(countryCode)) return;
            _data.CountryCode = countryCode;
            Save(createBackup: false);   // cosmetic; skip the per-tap backup copy (iOS disk I/O)
            OnProfileChanged?.Invoke();
        }

        private void Save(bool createBackup = true) => SaveSystem.Save(_data, SaveFile, 0, createBackup);

        private string DetectDeviceCountry()
        {
            try { return RegionInfo.CurrentRegion.TwoLetterISORegionName; }
            catch { return _catalog != null ? _catalog.defaultCountryCode : "US"; }
        }
    }
}
