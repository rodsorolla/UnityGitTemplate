using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sorolla.GoogleSheets
{
    /// <summary>
    /// Reflection-based schema for one ScriptableObject type.
    /// Discovers every field marked <see cref="SheetColumnAttribute"/> (public or private
    /// with SerializeField — we take both, since the project mixes styles) and exposes
    /// Read/Write helpers that convert between the typed field and the string cells used
    /// in the Sheets API.
    /// </summary>
    public class SheetSchema
    {
        public class Column
        {
            public string Name;
            public FieldInfo Field;
        }

        public readonly Type TargetType;
        public readonly List<Column> Columns = new();

        private static readonly Dictionary<Type, SheetSchema> _cache = new();

        public static SheetSchema For(Type type)
        {
            if (_cache.TryGetValue(type, out var cached)) return cached;
            var schema = Build(type);
            _cache[type] = schema;
            return schema;
        }

        private static SheetSchema Build(Type type)
        {
            var schema = new SheetSchema(type);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Walk base → derived so inherited columns come first. GetFields only returns declared fields
            // at each level, so we iterate the chain manually.
            var chain = new Stack<Type>();
            for (var t = type; t != null && t != typeof(UnityEngine.Object) && t != typeof(ScriptableObject); t = t.BaseType)
                chain.Push(t);

            while (chain.Count > 0)
            {
                var t = chain.Pop();
                foreach (var f in t.GetFields(flags | BindingFlags.DeclaredOnly))
                {
                    var attr = f.GetCustomAttribute<SheetColumnAttribute>();
                    if (attr == null) continue;
                    schema.Columns.Add(new Column { Name = attr.Name, Field = f });
                }
            }
            return schema;
        }

        private SheetSchema(Type t) { TargetType = t; }

        /// <summary>Read the annotated fields off <paramref name="instance"/> as strings in column order.</summary>
        public List<string> ReadRow(object instance)
        {
            var row = new List<string>(Columns.Count);
            foreach (var c in Columns)
                row.Add(ToCell(c.Field.GetValue(instance)));
            return row;
        }

        /// <summary>
        /// Write <paramref name="row"/> back into <paramref name="instance"/> using a SerializedObject,
        /// so the change is picked up by Unity's asset serialization (dirty-flag + undo + meta preserved).
        /// Returns the list of (column name, old → new) tuples that actually changed.
        /// </summary>
        public List<(string Column, string Before, string After)> WriteRow(
            UnityEngine.Object asset,
            IReadOnlyDictionary<string, string> rowByColumn)
        {
            var changes = new List<(string, string, string)>();
            var so = new SerializedObject(asset);
            bool anyChange = false;

            foreach (var c in Columns)
            {
                if (!rowByColumn.TryGetValue(c.Name, out var cell)) continue;

                var before = ToCell(c.Field.GetValue(asset));
                if (before == cell) continue;

                var prop = so.FindProperty(c.Field.Name);
                if (prop == null)
                {
                    // Field isn't serialized — fall back to direct reflection (e.g., public non-serialized).
                    try { c.Field.SetValue(asset, FromCell(cell, c.Field.FieldType)); }
                    catch (Exception e) { Debug.LogWarning($"[SheetSchema] {asset.name}.{c.Field.Name}: reflection set failed ({e.Message})"); continue; }
                }
                else
                {
                    if (!TrySetSerializedProperty(prop, cell, c.Field.FieldType))
                    {
                        Debug.LogWarning($"[SheetSchema] {asset.name}.{c.Field.Name}: unsupported SerializedProperty type {prop.propertyType}");
                        continue;
                    }
                }
                changes.Add((c.Name, before, cell));
                anyChange = true;
            }

            if (anyChange)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
            return changes;
        }

        // ---- Conversion helpers ----

        private static readonly CultureInfo INV = CultureInfo.InvariantCulture;

        public static string ToCell(object value)
        {
            if (value == null) return string.Empty;
            switch (value)
            {
                case string s: return s;
                case bool b: return b ? "TRUE" : "FALSE";
                case int i: return i.ToString(INV);
                case long l: return l.ToString(INV);
                case float f: return f.ToString("R", INV);
                case double d: return d.ToString("R", INV);
                case Enum e: return e.ToString();
                case Vector2 v2: return $"{v2.x.ToString("R", INV)},{v2.y.ToString("R", INV)}";
                case Vector3 v3: return $"{v3.x.ToString("R", INV)},{v3.y.ToString("R", INV)},{v3.z.ToString("R", INV)}";
                case UnityEngine.Object uo: return uo != null ? uo.name : string.Empty;
            }
            return value.ToString();
        }

        public static object FromCell(string cell, Type t)
        {
            cell ??= string.Empty;
            if (t == typeof(string)) return cell;
            if (t == typeof(bool)) return ParseBool(cell);
            if (t == typeof(int)) return int.TryParse(cell, NumberStyles.Integer, INV, out var i) ? i : 0;
            if (t == typeof(long)) return long.TryParse(cell, NumberStyles.Integer, INV, out var l) ? l : 0L;
            if (t == typeof(float)) return float.TryParse(cell, NumberStyles.Float, INV, out var f) ? f : 0f;
            if (t == typeof(double)) return double.TryParse(cell, NumberStyles.Float, INV, out var d) ? d : 0d;
            if (t.IsEnum)
            {
                if (string.IsNullOrWhiteSpace(cell)) return Activator.CreateInstance(t);
                try { return Enum.Parse(t, cell, ignoreCase: true); }
                catch { return Activator.CreateInstance(t); }
            }
            if (t == typeof(Vector2))
            {
                var p = cell.Split(',');
                return new Vector2(ParseFloat(p, 0), ParseFloat(p, 1));
            }
            if (t == typeof(Vector3))
            {
                var p = cell.Split(',');
                return new Vector3(ParseFloat(p, 0), ParseFloat(p, 1), ParseFloat(p, 2));
            }
            // Object references are resolved at a higher level (by-name) — not here.
            return null;
        }

        private static bool TrySetSerializedProperty(SerializedProperty prop, string cell, Type fieldType)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.String:
                    prop.stringValue = cell; return true;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = ParseBool(cell); return true;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                    prop.intValue = int.TryParse(cell, NumberStyles.Integer, INV, out var i) ? i : 0; return true;
                case SerializedPropertyType.Float:
                    prop.floatValue = float.TryParse(cell, NumberStyles.Float, INV, out var f) ? f : 0f; return true;
                case SerializedPropertyType.Enum:
                    if (fieldType.IsEnum && !string.IsNullOrWhiteSpace(cell))
                    {
                        try
                        {
                            var val = (int)Enum.Parse(fieldType, cell, ignoreCase: true);
                            prop.intValue = val;
                            return true;
                        }
                        catch { return false; }
                    }
                    return false;
                case SerializedPropertyType.Vector2:
                    { var p = cell.Split(','); prop.vector2Value = new Vector2(ParseFloat(p, 0), ParseFloat(p, 1)); return true; }
                case SerializedPropertyType.Vector3:
                    { var p = cell.Split(','); prop.vector3Value = new Vector3(ParseFloat(p, 0), ParseFloat(p, 1), ParseFloat(p, 2)); return true; }
                default:
                    return false;
            }
        }

        private static bool ParseBool(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            return s.Equals("true", StringComparison.OrdinalIgnoreCase)
                || s.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || s == "1"
                || s.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
        }

        private static float ParseFloat(string[] parts, int i)
        {
            if (parts == null || i >= parts.Length) return 0f;
            return float.TryParse(parts[i].Trim(), NumberStyles.Float, INV, out var v) ? v : 0f;
        }
    }
}
