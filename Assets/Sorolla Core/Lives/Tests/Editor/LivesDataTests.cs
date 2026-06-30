using System;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Sorolla.Lives.Tests
{
    public class LivesDataTests
    {
        [Test]
        public void Default_FreshInstance_IsZeroLivesEmptyTimestamps()
        {
            var d = new LivesData();
            Assert.That(d.current, Is.EqualTo(0));
            Assert.That(d.nextLifeAtUtcIso, Is.Null.Or.Empty);
            Assert.That(d.boosterUntilUtcIso, Is.Null.Or.Empty);
            Assert.That(d.lastSeenUtcIso, Is.Null.Or.Empty);
        }

        [Test]
        public void Version_IsOne()
        {
            Assert.That(new LivesData().Version, Is.EqualTo(1));
        }

        [Test]
        public void JsonRoundtrip_PreservesAllFields()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                DateFormatString = "o"
            };
            var src = new LivesData
            {
                current = 3,
                nextLifeAtUtcIso = "2026-05-06T10:30:00.0000000Z",
                boosterUntilUtcIso = "2026-05-06T11:00:00.0000000Z",
                lastSeenUtcIso = "2026-05-06T10:00:00.0000000Z"
            };

            var json = JsonConvert.SerializeObject(src, settings);
            var roundTripped = JsonConvert.DeserializeObject<LivesData>(json, settings);

            Assert.That(roundTripped.current, Is.EqualTo(3));
            Assert.That(roundTripped.nextLifeAtUtcIso, Is.EqualTo(src.nextLifeAtUtcIso));
            Assert.That(roundTripped.boosterUntilUtcIso, Is.EqualTo(src.boosterUntilUtcIso));
            Assert.That(roundTripped.lastSeenUtcIso, Is.EqualTo(src.lastSeenUtcIso));
        }
    }
}
