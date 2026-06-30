using NUnit.Framework;

namespace Sorolla.Events.Tests
{
    public class EventsSaveMigratorTests
    {
        [Test]
        public void Migrate_NullInput_ReturnsEmpty()
        {
            var result = EventsSaveMigrator.Migrate(null);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.instances.Count);
        }

        [Test]
        public void Migrate_V1Input_ReturnsSameInstance()
        {
            var input = new EventsSaveData();
            input.FindOrCreate("evt_a", "2026-05-15T12:00:00Z");
            var result = EventsSaveMigrator.Migrate(input);
            Assert.AreSame(input, result);
        }
    }
}
