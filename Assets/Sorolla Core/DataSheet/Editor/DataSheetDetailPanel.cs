using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sorolla.DataSheet.Editor
{
    /// <summary>
    /// Right-side panel showing the COMPLEX fields (lists, arrays, nested structs) of the
    /// selected asset — the fields the grid can only summarize. Each is a full PropertyField
    /// with native Undo. Scalars/object references are intentionally omitted (edit them in
    /// the grid). The ✕ button invokes onClose so the window can clear its selection.
    /// </summary>
    public static class DataSheetDetailPanel
    {
        const float PanelWidth = 320f;
        static Vector2 _scroll;

        public static void Draw(RowEntry row, List<string> allColumns, Action onClose)
        {
            // Defer the close until after the panel is fully drawn. Invoking onClose mid-layout
            // (or aborting via ExitGUI) risks a layout/repaint control-count mismatch for the frame.
            bool closeClicked = false;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(PanelWidth)))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label($"Detail · {row.name}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    closeClicked = GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(24));
                }

                row.so.Update();
                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                bool anyComplex = false;
                EditorGUI.BeginChangeCheck();
                foreach (var col in allColumns)
                {
                    var prop = row.so.FindProperty(col);
                    if (prop == null || !DataSheetValues.IsComplex(prop)) continue;
                    anyComplex = true;
                    EditorGUILayout.PropertyField(prop, true); // includeChildren -> full list/struct drawer
                    EditorGUILayout.Space(2);
                }
                if (EditorGUI.EndChangeCheck())
                    row.so.ApplyModifiedProperties(); // writes + registers native Undo

                if (!anyComplex)
                    EditorGUILayout.HelpBox("No list/struct fields on this type.", MessageType.Info);

                EditorGUILayout.EndScrollView();
            }

            if (closeClicked)
                onClose?.Invoke();
        }
    }
}
