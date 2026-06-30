using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sorolla.DataSheet.Editor
{
    /// <summary>A selectable ScriptableObject type plus its asset count.</summary>
    public struct TypeEntry
    {
        public Type type;
        public int count;
    }

    /// <summary>One asset row: the asset, its live SerializedObject, and its name.</summary>
    public class RowEntry
    {
        public UnityEngine.Object asset;
        public SerializedObject so;
        public string name;
    }

    /// <summary>
    /// Discovers ScriptableObject types that have assets, builds the column list for a type,
    /// and loads/filters asset rows. The only unit that touches reflection + AssetDatabase.
    /// </summary>
    public static class DataSheetModel
    {
        /// <summary>Concrete ScriptableObject types with at least one asset, sorted by name.</summary>
        public static List<TypeEntry> DiscoverTypes()
        {
            var result = new List<TypeEntry>();
            foreach (var t in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (t.IsAbstract || t.IsGenericType) continue;
                // Exclude Unity's own editor/settings SOs that derive from ScriptableObject in odd ways.
                if (typeof(EditorWindow).IsAssignableFrom(t) || typeof(UnityEditor.Editor).IsAssignableFrom(t)) continue;

                int count = AssetDatabase.FindAssets("t:" + t.Name).Length;
                if (count == 0) continue;
                result.Add(new TypeEntry { type = t, count = count });
            }
            result.Sort((a, b) => string.Compare(a.type.Name, b.type.Name, StringComparison.Ordinal));
            return result;
        }

        /// <summary>Top-level visible serialized property paths for a type, excluding m_Script.</summary>
        public static List<string> BuildColumns(Type type)
        {
            var cols = new List<string>();
            var temp = ScriptableObject.CreateInstance(type);
            try
            {
                var so = new SerializedObject(temp);
                var it = so.GetIterator();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = false; // stay at top-level siblings
                    if (it.propertyPath == "m_Script") continue;
                    cols.Add(it.propertyPath);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temp);
            }
            return cols;
        }

        /// <summary>
        /// Loads all assets of exactly <paramref name="type"/> as rows, sorted by name.
        /// Optionally filtered by a case-insensitive name substring.
        /// </summary>
        public static List<RowEntry> LoadRows(Type type, string search = null)
        {
            var rows = new List<RowEntry>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + type.Name))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, type);
                if (asset == null || asset.GetType() != type) continue; // exact type only
                if (!string.IsNullOrEmpty(search) &&
                    asset.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                rows.Add(new RowEntry { asset = asset, so = new SerializedObject(asset), name = asset.name });
            }
            rows.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            return rows;
        }
    }
}
