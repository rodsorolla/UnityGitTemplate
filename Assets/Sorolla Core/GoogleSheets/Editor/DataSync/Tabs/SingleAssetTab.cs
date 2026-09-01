using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sorolla.GoogleSheets.Tabs
{
    /// <summary>
    /// Base tab for a type that has exactly one asset (singleton-style config).
    /// Produces one header row + one data row. No add/delete semantics.
    /// </summary>
    public abstract class SingleAssetTab<T> : IDataSyncTab where T : ScriptableObject
    {
        public abstract string TabName { get; }

        private SheetSchema Schema => SheetSchema.For(typeof(T));

        public IReadOnlyList<string> Columns => Schema.Columns.Select(c => c.Name).ToList();

        protected virtual T FindAsset()
        {
            var list = TabUtils.FindAllAssets<T>();
            if (list.Count == 0) throw new System.Exception($"[{TabName}] No asset of type {typeof(T).Name} found in the project.");
            if (list.Count > 1) Debug.LogWarning($"[{TabName}] Multiple {typeof(T).Name} assets found — using first: {AssetDatabase.GetAssetPath(list[0])}");
            return list[0];
        }

        public List<List<string>> ReadFromAssets()
        {
            var asset = FindAsset();
            var rows = new List<List<string>> { Columns.ToList(), Schema.ReadRow(asset) };
            return rows;
        }

        public DiffReport BuildDiff(List<List<string>> sheetRows)
        {
            var report = new DiffReport { TabName = TabName };
            var (header, data) = TabUtils.SplitHeader(sheetRows);
            if (data.Count == 0) return report;

            var asset = FindAsset();
            var dict = TabUtils.RowToDict(header, data[0]);

            // Simulate the write without applying.
            foreach (var c in Schema.Columns)
            {
                if (!dict.TryGetValue(c.Name, out var cell)) continue;
                var before = SheetSchema.ToCell(c.Field.GetValue(asset));
                // Canonical compare — matches WriteToAssets' skip-if-equal logic exactly.
                var after = SheetSchema.Canonicalize(cell, c.Field.FieldType);
                if (before != after)
                {
                    if (report.Modifies.Count == 0)
                        report.Modifies.Add(new DiffReport.ModifiedRow { RowId = asset.name });
                    report.Modifies[0].FieldChanges.Add((c.Name, before, after));
                }
            }
            return report;
        }

        public void WriteToAssets(List<List<string>> sheetRows, bool allowDeletions)
        {
            var (header, data) = TabUtils.SplitHeader(sheetRows);
            if (data.Count == 0) { Debug.LogWarning($"[{TabName}] Sheet has no data row — skipping write."); return; }

            var asset = FindAsset();
            var dict = TabUtils.RowToDict(header, data[0]);
            var changes = Schema.WriteRow(asset, dict);
            Debug.Log($"[{TabName}] Applied {changes.Count} field change(s) to {asset.name}.");
        }
    }
}
