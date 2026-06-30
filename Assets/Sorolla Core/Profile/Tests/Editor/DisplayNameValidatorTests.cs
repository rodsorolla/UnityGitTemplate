using System.Collections.Generic;
using NUnit.Framework;
using Sorolla.Profile;

namespace Sorolla.Profile.Tests
{
    public class DisplayNameValidatorTests
    {
        [Test] public void Null_IsEmpty()
            => Assert.AreEqual(NameValidationResult.Empty, DisplayNameValidator.Validate(null));

        [Test] public void Whitespace_IsEmpty()
            => Assert.AreEqual(NameValidationResult.Empty, DisplayNameValidator.Validate("   "));

        [Test] public void TwoChars_IsTooShort()
            => Assert.AreEqual(NameValidationResult.TooShort, DisplayNameValidator.Validate("ab"));

        [Test] public void ThirteenChars_IsTooLong()
            => Assert.AreEqual(NameValidationResult.TooLong, DisplayNameValidator.Validate("abcdefghijklm"));

        [Test] public void Trimmed_LengthIsCounted()
            => Assert.AreEqual(NameValidationResult.Ok, DisplayNameValidator.Validate("  abc  "));

        [Test] public void Blocklist_MatchesCaseInsensitiveSubstring()
        {
            var list = new List<string> { "Badword" };
            Assert.AreEqual(NameValidationResult.Blocked, DisplayNameValidator.Validate("xxbadwordxx", list));
        }

        [Test] public void ValidName_IsOk()
            => Assert.AreEqual(NameValidationResult.Ok, DisplayNameValidator.Validate("Snakey"));

        [Test] public void MinLengthBoundary_IsOk()
            => Assert.AreEqual(NameValidationResult.Ok, DisplayNameValidator.Validate("abc"));

        [Test] public void MaxLengthBoundary_IsOk()
            => Assert.AreEqual(NameValidationResult.Ok, DisplayNameValidator.Validate("abcdefghijkl"));

        [Test] public void ZeroWidthChars_AreInvalid()      // U+200B padding renders blank
            => Assert.AreEqual(NameValidationResult.Invalid, DisplayNameValidator.Validate("a​​b"));

        [Test] public void BidiOverride_IsInvalid()          // U+202E scrambles row rendering
            => Assert.AreEqual(NameValidationResult.Invalid, DisplayNameValidator.Validate("ab‮cd"));

        [Test] public void ControlChar_IsInvalid()
            => Assert.AreEqual(NameValidationResult.Invalid, DisplayNameValidator.Validate("ab\tcd"));
    }
}
