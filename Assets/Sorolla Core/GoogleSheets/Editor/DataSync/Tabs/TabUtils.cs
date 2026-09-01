using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sorolla.GoogleSheets.Tabs
{
    /// <summary>
    /// Shared helpers for tab mappers.
    /// </summary>
    public static class TabUtils
    {
        /// <summary>Find all assets of type T in the project (or under a folder).</summary>
        public static List<T> FindAllAssets<T>(string folder = null) where T : Object
        {
            var filter = $"t:{typeof(T).Name}";
            var guids = folder != null
                ? AssetDatabase.FindAssets(filter, new[] { folder })
                : AssetDatabase.FindAssets(filter);
            var list = new List<T>();
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) list.Add(asset);
            }
            return list;
        }

        /// <summary>Convert a sheet row to a column-name → value dictionary using the header row.</summary>
        public static Dictionary<string, string> RowToDict(IReadOnlyList<string> header, IReadOnlyList<string> row)
        {
            var d = new Dictionary<string, string>();
            for (int i = 0; i < header.Count; i++)
            {
                var col = header[i];
                if (string.IsNullOrEmpty(col)) continue;
                d[col] = i < row.Count ? row[i] : string.Empty;
            }
            return d;
        }

        /// <summary>Split the sheet rows into a header and data rows. Returns (header, dataRows). Header is empty if sheet is empty.</summary>
        public static (List<string> header, List<List<string>> data) SplitHeader(List<List<string>> sheetRows)
        {
            if (sheetRows == null || sheetRows.Count == 0) return (new List<string>(), new List<List<string>>());
            var header = sheetRows[0];
            var data = sheetRows.Skip(1).ToList();
            return (header, data);
        }
    }
}
