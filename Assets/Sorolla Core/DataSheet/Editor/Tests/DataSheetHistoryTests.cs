using NUnit.Framework;
using Sorolla.DataSheet.Editor;

namespace Sorolla.DataSheet.EditorTests
{
    public class DataSheetHistoryTests
    {
        [Test]
        public void Record_StoresNewestFirst()
        {
            var h = new DataSheetHistory();
            h.Record(new ChangeEntry { assetName = "A", fieldPath = "hp", oldValue = "1", newValue = "2" });
            h.Record(new ChangeEntry { assetName = "B", fieldPath = "hp", oldValue = "3", newValue = "4" });

            Assert.AreEqual(2, h.Entries.Count);
            Assert.AreEqual("B", h.Entries[0].assetName); // newest first
            Assert.AreEqual("A", h.Entries[1].assetName);
        }

        [Test]
        public void RemoveAt_DropsEntry()
        {
            var h = new DataSheetHistory();
            h.Record(new ChangeEntry { assetName = "A", fieldPath = "hp", oldValue = "1", newValue = "2" });
            h.Record(new ChangeEntry { assetName = "B", fieldPath = "hp", oldValue = "3", newValue = "4" });

            h.RemoveAt(0); // drop "B"

            Assert.AreEqual(1, h.Entries.Count);
            Assert.AreEqual("A", h.Entries[0].assetName);
        }

        [Test]
        public void Clear_EmptiesLog()
        {
            var h = new DataSheetHistory();
            h.Record(new ChangeEntry { assetName = "A" });
            h.Clear();
            Assert.AreEqual(0, h.Entries.Count);
        }
    }
}
