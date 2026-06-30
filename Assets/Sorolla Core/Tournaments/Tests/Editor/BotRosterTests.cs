using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Sorolla.Profile;
using Sorolla.Tournaments;

namespace Sorolla.Tournaments.Tests
{
    public class BotRosterTests
    {
        ProfileCatalog _catalog;
        List<string> _names;
        TierDefinition _tier;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<ProfileCatalog>();
            _catalog.avatars.Add(new ProfileCatalog.AvatarEntry { id = "a1" });
            _catalog.avatars.Add(new ProfileCatalog.AvatarEntry { id = "a2" });
            _catalog.flags.Add(new ProfileCatalog.FlagEntry { countryCode = "US" });
            _catalog.flags.Add(new ProfileCatalog.FlagEntry { countryCode = "BR" });
            _names = new List<string> { "Alpha", "Bravo", "Charlie", "Delta" };
            _tier = new TierDefinition { groupSize = 10, botPaceMin = 5, botPaceMax = 40 };
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_catalog);

        static string Sig(List<Bot> bots)
        {
            var sb = new StringBuilder();
            foreach (var b in bots) sb.Append(b.id).Append(':').Append(b.displayName).Append(':')
                .Append(b.avatarId).Append(':').Append(b.countryCode).Append(':').Append(b.weeklyTarget).Append('|');
            return sb.ToString();
        }

        [Test]
        public void Build_SameSeed_IsIdentical()
        {
            var a = BotRoster.Build(0, 5, _tier, _catalog, _names);
            var b = BotRoster.Build(0, 5, _tier, _catalog, _names);
            Assert.AreEqual(Sig(a), Sig(b));
        }

        [Test]
        public void Build_Count_IsGroupSizeMinusOne()
            => Assert.AreEqual(9, BotRoster.Build(0, 5, _tier, _catalog, _names).Count);

        [Test]
        public void Build_TargetsWithinBand()
        {
            foreach (var b in BotRoster.Build(0, 5, _tier, _catalog, _names))
            {
                Assert.GreaterOrEqual(b.weeklyTarget, 5);
                Assert.LessOrEqual(b.weeklyTarget, 40);
            }
        }

        [Test]
        public void Build_DifferentWeek_DiffersSomewhere()
        {
            var w0 = BotRoster.Build(0, 0, _tier, _catalog, _names);
            var w1 = BotRoster.Build(0, 1, _tier, _catalog, _names);
            Assert.AreNotEqual(Sig(w0), Sig(w1));
        }
    }
}
