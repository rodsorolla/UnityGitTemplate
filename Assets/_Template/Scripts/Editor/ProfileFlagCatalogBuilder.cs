using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Sorolla.Profile;
using UnityEditor;
using UnityEngine;

namespace HungrySnake.Editor
{
    /// <summary>
    /// Builds/refreshes the ProfileCatalog flag list from the imported flag sprites
    /// in Assets/_Game/Sprites/Flags. Country codes are stored UPPERCASE to match
    /// PlayerProfileService.DetectDeviceCountry (RegionInfo.TwoLetterISORegionName).
    /// Display names are resolved via .NET RegionInfo, falling back to the code.
    /// Re-runnable: rebuilds the flags list in place, preserves avatars/other fields.
    /// </summary>
    public static class ProfileFlagCatalogBuilder
    {
        private const string FlagsDir = "Assets/_Game/Sprites/Flags";
        private const string AvatarsDir = "Assets/_Game/Sprites/Avatars";
        private const string CatalogDir = "Assets/_Game/Data/Profile";
        private const string CatalogPath = CatalogDir + "/ProfileCatalog.asset";

        // Curated set of 50 countries surfaced in the profile flag picker. The Flags folder
        // holds ~250 sprites; only these are added to the catalog. Codes are UPPERCASE ISO-3166
        // alpha-2 to match the country codes stored on flag entries. Edit this list to change
        // which flags appear, then re-run Tools/Sorolla/Profile/Build Flag Catalog.
        private static readonly HashSet<string> AllowedCountries = new HashSet<string>
        {
            // Americas
            "US", "CA", "MX", "BR", "AR", "CL", "CO", "PE",
            // Western & Central Europe
            "GB", "IE", "FR", "DE", "IT", "ES", "PT", "NL", "BE", "CH", "AT",
            // Northern & Eastern Europe
            "SE", "NO", "DK", "FI", "PL", "CZ", "HU", "RO", "GR", "UA", "RU",
            // Asia
            "CN", "JP", "KR", "IN", "ID", "PH", "TH", "VN", "MY", "SG",
            // Oceania
            "AU", "NZ",
            // Middle East & Africa
            "TR", "SA", "AE", "IL", "EG", "ZA", "NG", "MA",
        };

        [MenuItem("Tools/Sorolla/Profile/Build Flag Catalog")]
        public static void Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ProfileCatalog>(CatalogPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(CatalogDir);
                catalog = ScriptableObject.CreateInstance<ProfileCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var names = BuildNameMap();
            var entries = new List<ProfileCatalog.FlagEntry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { FlagsDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                var code = Path.GetFileNameWithoutExtension(path).ToUpperInvariant();
                if (!AllowedCountries.Contains(code)) continue; // keep only the curated 50
                entries.Add(new ProfileCatalog.FlagEntry
                {
                    countryCode = code,
                    displayName = names.TryGetValue(code, out var n) ? n : code,
                    sprite = sprite,
                    locked = false
                });
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.displayName, b.displayName));
            catalog.flags = entries;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ProfileFlagCatalogBuilder] Wrote {entries.Count} flag entries to {CatalogPath}");
        }

        /// <summary>
        /// Builds/refreshes the ProfileCatalog avatar list from the sprites in
        /// Assets/_Game/Sprites/Avatars. The avatar id is the numeric suffix of the
        /// filename (e.g. SnakeAvatar_00007 -> "7"). Re-runnable: rebuilds the avatars
        /// list in place, preserves the flags/other fields.
        /// </summary>
        [MenuItem("Tools/Sorolla/Profile/Build Avatar Catalog")]
        public static void BuildAvatars()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ProfileCatalog>(CatalogPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(CatalogDir);
                catalog = ScriptableObject.CreateInstance<ProfileCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var entries = new List<ProfileCatalog.AvatarEntry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { AvatarsDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                entries.Add(new ProfileCatalog.AvatarEntry
                {
                    id = AvatarIdFromFileName(Path.GetFileNameWithoutExtension(path)),
                    sprite = sprite,
                    locked = false
                });
            }

            // Numeric order: "2" before "10".
            entries.Sort((a, b) => ParseIdOrMax(a.id).CompareTo(ParseIdOrMax(b.id)));
            catalog.avatars = entries;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ProfileFlagCatalogBuilder] Wrote {entries.Count} avatar entries to {CatalogPath}");
        }

        // "SnakeAvatar_00007" -> "7". Falls back to the whole name if there's no numeric suffix.
        private static string AvatarIdFromFileName(string fileName)
        {
            int i = fileName.Length;
            while (i > 0 && char.IsDigit(fileName[i - 1])) i--;
            var digits = fileName.Substring(i);
            return digits.Length > 0 && int.TryParse(digits, out var n) ? n.ToString() : fileName;
        }

        private static int ParseIdOrMax(string id) => int.TryParse(id, out var n) ? n : int.MaxValue;

        // Derive code -> English country name from every specific culture the
        // runtime knows. Far broader coverage than constructing RegionInfo per code,
        // and avoids a hand-maintained name table.
        private static Dictionary<string, string> BuildNameMap()
        {
            var map = new Dictionary<string, string>();
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                try
                {
                    var region = new RegionInfo(culture.Name);
                    var code = region.TwoLetterISORegionName.ToUpperInvariant();
                    if (!map.ContainsKey(code)) map[code] = region.EnglishName;
                }
                catch { /* skip cultures without a resolvable region */ }
            }
            // Fill regions the trimmed Unity runtime has no culture for.
            foreach (var kv in Overrides())
                if (!map.ContainsKey(kv.Key)) map[kv.Key] = kv.Value;
            return map;
        }

        // Explicit names for ISO codes the runtime can't resolve via culture data.
        private static Dictionary<string, string> Overrides()
        {
            var d = new Dictionary<string, string>();
            d["AD"] = "Andorra"; d["AG"] = "Antigua & Barbuda"; d["AI"] = "Anguilla";
            d["AO"] = "Angola"; d["AQ"] = "Antarctica"; d["AS"] = "American Samoa";
            d["AW"] = "Aruba"; d["AX"] = "Aland Islands"; d["BB"] = "Barbados";
            d["BF"] = "Burkina Faso"; d["BI"] = "Burundi"; d["BJ"] = "Benin";
            d["BL"] = "Saint Barthelemy"; d["BM"] = "Bermuda"; d["BQ"] = "Caribbean Netherlands";
            d["BS"] = "Bahamas"; d["BT"] = "Bhutan"; d["BV"] = "Bouvet Island";
            d["CC"] = "Cocos (Keeling) Islands"; d["CF"] = "Central African Republic";
            d["CG"] = "Congo - Brazzaville"; d["CK"] = "Cook Islands"; d["CV"] = "Cape Verde";
            d["CW"] = "Curacao"; d["CX"] = "Christmas Island"; d["CY"] = "Cyprus";
            d["DJ"] = "Djibouti"; d["DM"] = "Dominica"; d["EH"] = "Western Sahara";
            d["FJ"] = "Fiji"; d["FK"] = "Falkland Islands"; d["FM"] = "Micronesia";
            d["GA"] = "Gabon"; d["GD"] = "Grenada"; d["GF"] = "French Guiana";
            d["GG"] = "Guernsey"; d["GH"] = "Ghana"; d["GI"] = "Gibraltar";
            d["GM"] = "Gambia"; d["GN"] = "Guinea"; d["GP"] = "Guadeloupe";
            d["GQ"] = "Equatorial Guinea"; d["GS"] = "South Georgia & South Sandwich Islands";
            d["GU"] = "Guam"; d["GW"] = "Guinea-Bissau"; d["GY"] = "Guyana";
            d["HM"] = "Heard & McDonald Islands"; d["IM"] = "Isle of Man";
            d["IO"] = "British Indian Ocean Territory"; d["JE"] = "Jersey";
            d["KI"] = "Kiribati"; d["KM"] = "Comoros"; d["KN"] = "Saint Kitts & Nevis";
            d["KP"] = "North Korea"; d["KY"] = "Cayman Islands"; d["LC"] = "Saint Lucia";
            d["LR"] = "Liberia"; d["LS"] = "Lesotho"; d["MF"] = "Saint Martin";
            d["MG"] = "Madagascar"; d["MH"] = "Marshall Islands"; d["MP"] = "Northern Mariana Islands";
            d["MQ"] = "Martinique"; d["MR"] = "Mauritania"; d["MS"] = "Montserrat";
            d["MU"] = "Mauritius"; d["MV"] = "Maldives"; d["MW"] = "Malawi";
            d["MZ"] = "Mozambique"; d["NA"] = "Namibia"; d["NC"] = "New Caledonia";
            d["NE"] = "Niger"; d["NF"] = "Norfolk Island"; d["NR"] = "Nauru"; d["NU"] = "Niue";
            d["PF"] = "French Polynesia"; d["PG"] = "Papua New Guinea";
            d["PM"] = "Saint Pierre & Miquelon"; d["PN"] = "Pitcairn Islands";
            d["PS"] = "Palestinian Territories"; d["PW"] = "Palau"; d["SB"] = "Solomon Islands";
            d["SC"] = "Seychelles"; d["SD"] = "Sudan"; d["SH"] = "Saint Helena";
            d["SJ"] = "Svalbard & Jan Mayen"; d["SL"] = "Sierra Leone"; d["SM"] = "San Marino";
            d["SR"] = "Suriname"; d["SS"] = "South Sudan"; d["ST"] = "Sao Tome & Principe";
            d["SX"] = "Sint Maarten"; d["SZ"] = "Eswatini";
            d["TC"] = "Turks & Caicos Islands"; d["TD"] = "Chad";
            d["TF"] = "French Southern Territories"; d["TG"] = "Togo"; d["TK"] = "Tokelau";
            d["TL"] = "Timor-Leste"; d["TO"] = "Tonga"; d["TV"] = "Tuvalu"; d["TZ"] = "Tanzania";
            d["UG"] = "Uganda"; d["UM"] = "U.S. Outlying Islands"; d["VA"] = "Vatican City";
            d["VC"] = "Saint Vincent & Grenadines"; d["VG"] = "British Virgin Islands";
            d["VI"] = "U.S. Virgin Islands"; d["VU"] = "Vanuatu"; d["WF"] = "Wallis & Futuna";
            d["WS"] = "Samoa"; d["YT"] = "Mayotte"; d["ZM"] = "Zambia";
            return d;
        }
    }
}
