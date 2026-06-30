using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Sorolla.DataSheet.Editor
{
    /// <summary>
    /// Converts a scalar <see cref="SerializedProperty"/> to/from a string.
    /// Single source of truth shared by export, import, and history capture so
    /// the same scalar formatting/parsing rules apply everywhere.
    /// Non-scalar properties (object references, arrays, structs) are unsupported.
    /// </summary>
    public static class DataSheetValues
    {
        public static bool IsScalar(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Color:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// True for properties the grid can't edit inline — arrays, lists, and nested
        /// structs. Scalars and object references are editable in a cell; everything
        /// else belongs in the detail panel.
        /// </summary>
        public static bool IsComplex(SerializedProperty p)
        {
            return !IsScalar(p) && p.propertyType != SerializedPropertyType.ObjectReference;
        }

        /// <summary>Reads a scalar property as an invariant-culture string. Returns "" for non-scalars.</summary>
        public static string ReadScalar(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return p.intValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Boolean:
                    return p.boolValue ? "true" : "false";
                case SerializedPropertyType.Float:
                    return p.floatValue.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.String:
                    return p.stringValue ?? "";
                case SerializedPropertyType.Enum:
                    return (p.enumValueIndex >= 0 && p.enumValueIndex < p.enumNames.Length)
                        ? p.enumNames[p.enumValueIndex]
                        : "";
                case SerializedPropertyType.Color:
                    return "#" + ColorUtility.ToHtmlStringRGBA(p.colorValue);
                default:
                    return "";
            }
        }

        /// <summary>
        /// Writes a string into a scalar property. Does NOT call ApplyModifiedProperties.
        /// Returns false (and leaves the property unchanged) if the value can't be parsed
        /// or the property isn't a supported scalar.
        /// </summary>
        public static bool WriteScalar(SerializedProperty p, string raw)
        {
            raw ??= "";
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        p.intValue = i;
                        return true;
                    }
                    return false;
                case SerializedPropertyType.Boolean:
                    if (bool.TryParse(raw, out var b))
                    {
                        p.boolValue = b;
                        return true;
                    }
                    return false;
                case SerializedPropertyType.Float:
                    if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    {
                        p.floatValue = f;
                        return true;
                    }
                    return false;
                case SerializedPropertyType.String:
                    p.stringValue = raw;
                    return true;
                case SerializedPropertyType.Enum:
                    var idx = Array.IndexOf(p.enumNames, raw);
                    if (idx >= 0)
                    {
                        p.enumValueIndex = idx;
                        return true;
                    }
                    return false;
                case SerializedPropertyType.Color:
                    if (ColorUtility.TryParseHtmlString(raw, out var c))
                    {
                        p.colorValue = c;
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }
    }
}
