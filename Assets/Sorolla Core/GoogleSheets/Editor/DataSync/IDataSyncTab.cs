using System.Collections.Generic;

namespace Sorolla.GoogleSheets
{
    /// <summary>
    /// One tab in the sync tool = one sheet tab + one set of ScriptableObject assets.
    /// Implementations live under <c>Tabs/</c> and are registered in <see cref="DataSyncWindow"/>.
    /// </summary>
    public interface IDataSyncTab
    {
        /// <summary>Sheet tab name. Also used in Sheets API ranges (e.g. "Enemies!A1:Z").</summary>
        string TabName { get; }

        /// <summary>Column headers in row 1 order. Must match what Push/Pull expect.</summary>
        IReadOnlyList<string> Columns { get; }

        /// <summary>Build rows from the current assets on disk. Row 0 = header; subsequent rows = data.</summary>
        List<List<string>> ReadFromAssets();

        /// <summary>
        /// Given rows pulled from the sheet (row 0 = header), compute what would change on disk.
        /// Pure — does not modify assets.
        /// </summary>
        DiffReport BuildDiff(List<List<string>> sheetRows);

        /// <summary>
        /// Apply sheet rows to disk. Respects <paramref name="allowDeletions"/>; if false, missing rows are reported skipped, not deleted.
        /// </summary>
        void WriteToAssets(List<List<string>> sheetRows, bool allowDeletions);
    }
}
