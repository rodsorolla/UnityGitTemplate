using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.Profile
{
    [CreateAssetMenu(fileName = "ProfileCatalog", menuName = "Sorolla/Profile/Profile Catalog")]
    public class ProfileCatalog : ScriptableObject
    {
        [Serializable]
        public class AvatarEntry
        {
            public string id;
            public Sprite sprite;
            public bool locked; // dormant in v1 — forward-compat for gating
        }

        [Serializable]
        public class FlagEntry
        {
            public string countryCode;
            public string displayName;
            public Sprite sprite;
            public bool locked; // dormant in v1
        }

        public List<AvatarEntry> avatars = new List<AvatarEntry>();
        public List<FlagEntry> flags = new List<FlagEntry>();

        [Tooltip("Used when the device region isn't present in the flags list.")]
        public string defaultCountryCode = "US";

        [Tooltip("Case-insensitive substrings rejected in display names. May be empty.")]
        public List<string> nameBlocklist = new List<string>();

        public bool HasAvatar(string id)
        {
            foreach (var a in avatars) if (a != null && a.id == id) return true;
            return false;
        }

        public bool HasFlag(string countryCode)
        {
            foreach (var f in flags) if (f != null && f.countryCode == countryCode) return true;
            return false;
        }

        public string FirstAvatarId => avatars.Count > 0 && avatars[0] != null ? avatars[0].id : null;

        public Sprite GetAvatarSprite(string id)
        {
            foreach (var a in avatars) if (a != null && a.id == id) return a.sprite;
            return null;
        }

        public Sprite GetFlagSprite(string countryCode)
        {
            foreach (var f in flags) if (f != null && f.countryCode == countryCode) return f.sprite;
            return null;
        }
    }
}
