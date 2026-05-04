using System.Collections.Generic;
using System.Text;

namespace Sorolla.GoogleSheets
{
    /// <summary>
    /// Per-tab preview of what a Pull would change.
    /// Produced by a tab mapper's diff pass before any write happens — shown to the
    /// user in the sync window so they can confirm.
    /// </summary>
    public class DiffReport
    {
        public string TabName;

        /// <summary>Rows present in the sheet but not yet on disk.</summary>
        public List<string> Adds = new();

        /// <summary>Rows present on disk but missing from the sheet. Only applied if AllowDeletionsOnPull.</summary>
        public List<string> Deletes = new();

        /// <summary>Rows present in both, with at least one column changed. Key = row id; value = per-column changes.</summary>
        public List<ModifiedRow> Modifies = new();

        public class ModifiedRow
        {
            public string RowId;
            public List<(string Column, string Before, string After)> FieldChanges = new();
        }

        public bool HasChanges => Adds.Count > 0 || Deletes.Count > 0 || Modifies.Count > 0;

        public string Summarize()
        {
            var sb = new StringBuilder();
            sb.Append($"[{TabName}] ");
            if (!HasChanges) { sb.Append("no changes"); return sb.ToString(); }
            sb.Append($"{Adds.Count} add, {Modifies.Count} mod, {Deletes.Count} del");
            return sb.ToString();
        }

        public string Details()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"== {TabName} ==");
            foreach (var a in Adds) sb.AppendLine($"  + {a}");
            foreach (var d in Deletes) sb.AppendLine($"  - {d}");
            foreach (var m in Modifies)
            {
                sb.AppendLine($"  ~ {m.RowId}");
                foreach (var (col, before, after) in m.FieldChanges)
                    sb.AppendLine($"      {col}: {before}  →  {after}");
            }
            return sb.ToString();
        }
    }
}
