using System.Collections.Generic;
using NUnit.Framework;
using Sorolla.DataSheet.Editor;

namespace Sorolla.DataSheet.EditorTests
{
    public class DataSheetIOTests
    {
        static SheetTable SampleTable()
        {
            return new SheetTable
            {
                Columns = new List<string> { "Name", "hp", "label" },
                Rows = new List<List<string>>
                {
                    new List<string> { "IronSword", "12", "basic sword" },
                    new List<string> { "Excalibur", "50", "the, legendary \"one\"" },
                }
            };
        }

        [Test]
        public void Csv_RoundTrips_IncludingQuotingAndCommas()
        {
            var table = SampleTable();
            var csv = DataSheetIO.ToCsv(table);
            var parsed = DataSheetIO.ParseCsv(csv);

            Assert.AreEqual(table.Columns, parsed.Columns);
            Assert.AreEqual(2, parsed.Rows.Count);
            Assert.AreEqual(table.Rows[0], parsed.Rows[0]);
            Assert.AreEqual(table.Rows[1], parsed.Rows[1]); // embedded comma + quotes survive
        }

        [Test]
        public void Json_RoundTrips()
        {
            var table = SampleTable();
            var json = DataSheetIO.ToJson(table);
            var parsed = DataSheetIO.ParseJson(json);

            Assert.AreEqual(table.Columns, parsed.Columns);
            Assert.AreEqual(2, parsed.Rows.Count);
            Assert.AreEqual(table.Rows[1], parsed.Rows[1]);
        }

        [Test]
        public void Diff_MatchesByNameColumn_AndReportsChanges()
        {
            var current = SampleTable();
            var imported = SampleTable();
            imported.Rows[0][1] = "99"; // IronSword hp 12 -> 99

            var diff = DataSheetIO.Diff(current, imported);

            Assert.AreEqual(1, diff.Changes.Count);
            Assert.AreEqual("IronSword", diff.Changes[0].rowKey);
            Assert.AreEqual("hp", diff.Changes[0].column);
            Assert.AreEqual("12", diff.Changes[0].oldValue);
            Assert.AreEqual("99", diff.Changes[0].newValue);
            Assert.AreEqual(0, diff.UnmatchedRows.Count);
        }

        [Test]
        public void Diff_ReportsUnmatchedRows_AndIgnoresUnknownColumns()
        {
            var current = SampleTable();
            var imported = SampleTable();
            imported.Rows.Add(new List<string> { "GhostBlade", "7", "missing asset" });
            imported.Columns.Add("unknownCol");
            imported.Rows[0].Add("x"); // value under unknownCol — must be ignored
            imported.Rows[1].Add("y");

            var diff = DataSheetIO.Diff(current, imported);

            Assert.Contains("GhostBlade", diff.UnmatchedRows);
            Assert.IsFalse(diff.Changes.Exists(c => c.column == "unknownCol"));
        }
    }
}
