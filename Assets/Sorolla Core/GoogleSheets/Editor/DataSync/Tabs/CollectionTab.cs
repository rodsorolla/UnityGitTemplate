using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sorolla.GoogleSheets.Tabs
{
    /// <summary>
    /// Base tab for a collection of ScriptableObjects that share a base type but have multiple
    /// concrete subclasses (e.g. DefenderData → Cone/SingleTarget/Piercing/Explosion).
    ///
    /// Each row has two virtual columns — <c>AssetName</c> (filename, row id) and
    /// <c>Subclass</c> (concrete C# type name) — followed by the union of every subclass's
    /// schema columns.
    /// </summary>
    public abstract class CollectionTab<TBase> : IDataSyncTab where TBase : ScriptableObject
    {
        public abstract string TabName { get; }

        /// <summary>Folder under which new assets are created on Pull (e.g. "Assets/_Game/Data/Enemies").</summary>
        protected abstract string NewAssetFolder { get; }

        /// <summary>Allowed concrete types for the "Subclass" column. Rows with other subclasses are skipped.</summary>
        protected abstract IReadOnlyList<Type> ConcreteTypes { get; }

        private List<string> _columnsCached;
        public IReadOnlyList<string> Columns
        {
            get
            {
                if (_columnsCached != null) return _columnsCached;
                var cols = new List<string> { "AssetName", "Subclass" };
                var seen = new HashSet<string>(cols);
                foreach (var t in ConcreteTypes)
                {
                    foreach (var c in SheetSchema.For(t).Columns)
                    {
                        if (seen.Add(c.Name)) cols.Add(c.Name);
                    }
                }
                _columnsCached = cols;
                return _columnsCached;
            }
        }

        public List<List<string>> ReadFromAssets()
        {
            var assets = TabUtils.FindAllAssets<TBase>();
            var rows = new List<List<string>> { Columns.ToList() };

            foreach (var asset in assets.OrderBy(a => a.name, StringComparer.OrdinalIgnoreCase))
            {
                var schema = SheetSchema.For(asset.GetType());
                var fieldByName = schema.Columns.ToDictionary(c => c.Name, c => c.Field);

                var row = new List<string>(Columns.Count);
                foreach (var col in Columns)
                {
                    if (col == "AssetName") { row.Add(asset.name); continue; }
                    if (col == "Subclass") { row.Add(asset.GetType().Name); continue; }
                    row.Add(fieldByName.TryGetValue(col, out var f) ? SheetSchema.ToCell(f.GetValue(asset)) : string.Empty);
                }
                rows.Add(row);
            }
            return rows;
        }

        public DiffReport BuildDiff(List<List<string>> sheetRows)
        {
            var report = new DiffReport { TabName = TabName };
            var (header, data) = TabUtils.SplitHeader(sheetRows);

            var existing = TabUtils.FindAllAssets<TBase>().ToDictionary(a => a.name, a => a);
            var sheetNames = new HashSet<string>();

            foreach (var row in data)
            {
                var dict = TabUtils.RowToDict(header, row);
                if (!dict.TryGetValue("AssetName", out var name) || string.IsNullOrWhiteSpace(name)) continue;
                sheetNames.Add(name);

                if (!existing.TryGetValue(name, out var asset))
                {
                    report.Adds.Add(name);
                    continue;
                }

                var schema = SheetSchema.For(asset.GetType());
                var mods = new DiffReport.ModifiedRow { RowId = name };
                foreach (var c in schema.Columns)
                {
                    if (!dict.TryGetValue(c.Name, out var cell)) continue;
                    var before = SheetSchema.ToCell(c.Field.GetValue(asset));
                    if (before != cell) mods.FieldChanges.Add((c.Name, before, cell));
                }
                if (mods.FieldChanges.Count > 0) report.Modifies.Add(mods);
            }

            foreach (var name in existing.Keys)
                if (!sheetNames.Contains(name)) report.Deletes.Add(name);

            return report;
        }

        public void WriteToAssets(List<List<string>> sheetRows, bool allowDeletions)
        {
            var (header, data) = TabUtils.SplitHeader(sheetRows);
            var existing = TabUtils.FindAllAssets<TBase>().ToDictionary(a => a.name, a => a);
            var sheetNames = new HashSet<string>();
            var subclassByName = ConcreteTypes.ToDictionary(t => t.Name, t => t);

            int adds = 0, mods = 0, dels = 0, skipped = 0;

            foreach (var row in data)
            {
                var dict = TabUtils.RowToDict(header, row);
                if (!dict.TryGetValue("AssetName", out var name) || string.IsNullOrWhiteSpace(name)) continue;
                sheetNames.Add(name);
                dict.TryGetValue("Subclass", out var subclass);

                if (!existing.TryGetValue(name, out var asset))
                {
                    if (string.IsNullOrEmpty(subclass) || !subclassByName.TryGetValue(subclass, out var type))
                    {
                        Debug.LogWarning($"[{TabName}] Row '{name}': missing/unknown Subclass '{subclass}' — skipping add.");
                        skipped++; continue;
                    }
                    asset = (TBase)ScriptableObject.CreateInstance(type);
                    if (!AssetDatabase.IsValidFolder(NewAssetFolder))
                        EnsureFolder(NewAssetFolder);
                    var path = $"{NewAssetFolder}/{name}.asset";
                    AssetDatabase.CreateAsset(asset, path);
                    adds++;
                }

                var schema = SheetSchema.For(asset.GetType());
                if (schema.WriteRow(asset, dict).Count > 0) mods++;
            }

            if (allowDeletions)
            {
                foreach (var kvp in existing)
                {
                    if (sheetNames.Contains(kvp.Key)) continue;
                    var path = AssetDatabase.GetAssetPath(kvp.Value);
                    AssetDatabase.DeleteAsset(path);
                    dels++;
                }
            }
            else
            {
                foreach (var name in existing.Keys)
                    if (!sheetNames.Contains(name)) skipped++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[{TabName}] +{adds} ~{mods} -{dels} (skipped {skipped}).");
        }

        private static void EnsureFolder(string folder)
        {
            var parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
