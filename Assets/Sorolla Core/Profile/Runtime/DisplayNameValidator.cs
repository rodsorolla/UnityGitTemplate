using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sorolla.Profile
{
    public static class DisplayNameValidator
    {
        public const int MinLength = 3;
        public const int MaxLength = 12;

        public static NameValidationResult Validate(string rawName, IReadOnlyList<string> blocklist = null)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return NameValidationResult.Empty;

            string name = rawName.Trim();

            if (name.Length < MinLength) return NameValidationResult.TooShort;
            if (name.Length > MaxLength) return NameValidationResult.TooLong;

            // Reject control chars and Unicode "format" chars (zero-width spaces, bidi marks /
            // RTL-override) — they pass the length check but render blank or scramble rows.
            foreach (char c in name)
                if (char.IsControl(c) || char.GetUnicodeCategory(c) == UnicodeCategory.Format)
                    return NameValidationResult.Invalid;

            if (blocklist != null)
            {
                foreach (var word in blocklist)
                {
                    if (string.IsNullOrEmpty(word)) continue;
                    if (name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                        return NameValidationResult.Blocked;
                }
            }

            return NameValidationResult.Ok;
        }
    }
}
