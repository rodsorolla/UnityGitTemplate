using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sorolla.DataSheet.Editor
{
    /// <summary>
    /// Draws the spreadsheet grid for a page of rows and a set of visible columns.
    /// Each cell is a PropertyField on the asset's SerializedProperty, giving real
    /// drawers + native Undo. Scalar edits are captured into the supplied history.
    /// </summary>
    public static class DataSheetTable
    {
        const float SelectColWidth = 22f;
        const float NameColWidth = 180f;
        const float CellWidth = 160f;
        const float RowHeight = 20f;

        static Vector2 _scroll;
        static GUIStyle _selectedRow;
        static GUIStyle SelectedRow => _selectedRow ??= new GUIStyle(GUI.skin.FindStyle("SelectionRect") ?? GUI.skin.box);

        public static RowEntry Draw(List<string> visibleColumns, List<RowEntry> pageRows,
                                    DataSheetHistory history, RowEntry selected)
        {
            if (visibleColumns == null || visibleColumns.Count == 0)
            {
                EditorGUILayout.HelpBox("No columns visible. Use Columns ▾ to enable some.", MessageType.Info);
                return selected;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // Header
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(GUIContent.none, GUILayout.Width(SelectColWidth)); // ▸ column spacer
                GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(NameColWidth));
                foreach (var col in visibleColumns)
                    GUILayout.Label(ObjectNames.NicifyVariableName(col), EditorStyles.boldLabel, GUILayout.Width(CellWidth));
            }

            // Rows
            foreach (var row in pageRows)
            {
                row.so.Update();
                bool isSelected = ReferenceEquals(row, selected);
                using (new EditorGUILayout.HorizontalScope(isSelected ? SelectedRow : GUIStyle.none))
                {
                    if (GUILayout.Button("▸", EditorStyles.miniButton, GUILayout.Width(SelectColWidth), GUILayout.Height(RowHeight)))
                        selected = row;

                    GUILayout.Label(row.name, isSelected ? EditorStyles.boldLabel : EditorStyles.label,
                        GUILayout.Width(NameColWidth), GUILayout.Height(RowHeight));

                    foreach (var col in visibleColumns)
                    {
                        var prop = row.so.FindProperty(col);
                        if (prop == null)
                        {
                            GUILayout.Label("—", GUILayout.Width(CellWidth));
                            continue;
                        }
                        DrawCell(row, prop, history);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            return selected;
        }

        static void DrawCell(RowEntry row, SerializedProperty prop, DataSheetHistory history)
        {
            bool scalar = DataSheetValues.IsScalar(prop);

            // Only scalars and object references fit a fixed-width cell. Arrays and nested
            // structs would expand into foldouts and break the grid, so show a compact
            // read-only summary instead (these are also skipped by export/import).
            if (DataSheetValues.IsComplex(prop))
            {
                GUILayout.Label(NonEditableSummary(prop), EditorStyles.miniLabel, GUILayout.Width(CellWidth));
                return;
            }

            string before = scalar ? DataSheetValues.ReadScalar(prop) : null;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(prop, GUIContent.none, false, GUILayout.Width(CellWidth));
            if (EditorGUI.EndChangeCheck())
            {
                row.so.ApplyModifiedProperties(); // writes + registers native Undo
                if (scalar)
                {
                    string after = DataSheetValues.ReadScalar(prop);
                    if (after != before)
                    {
                        history.Record(new ChangeEntry
                        {
                            assetName = row.name,
                            fieldPath = prop.propertyPath,
                            oldValue = before,
                            newValue = after,
                            timestamp = DateTime.Now.ToString("HH:mm:ss")
                        });
                    }
                }
            }
        }

        /// <summary>Compact label for columns that can't be edited inline (arrays, nested structs).</summary>
        static string NonEditableSummary(SerializedProperty prop)
        {
            return prop.isArray ? $"[{prop.arraySize}]" : $"({prop.propertyType})";
        }
    }
}
