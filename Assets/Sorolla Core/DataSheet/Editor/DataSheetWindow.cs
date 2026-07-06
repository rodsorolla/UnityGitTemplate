using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sorolla.DataSheet.Editor
{
    /// <summary>
    /// DataSheet: edit every asset of a chosen ScriptableObject type as a spreadsheet.
    /// Rows = assets, columns = serialized fields, inline editing via real property drawers.
    /// Menu: Tools/Sorolla/DataSheet.
    /// </summary>
    public class DataSheetWindow : EditorWindow
    {
        const int PageSize = 50;
        const string HiddenColsPrefKey = "Sorolla.DataSheet.HiddenCols."; // + type full name

        // Runtime caches — these hold System.Type / live SerializedObjects that can't survive
        // a Unity domain reload, so they are rebuilt in OnEnable rather than serialized.
        [NonSerialized] List<TypeEntry> _types = new List<TypeEntry>();
        [NonSerialized] string[] _typeLabels = Array.Empty<string>();
        [NonSerialized] Type _selectedType;
        [NonSerialized] List<string> _allColumns = new List<string>();
        [NonSerialized] HashSet<string> _hiddenColumns = new HashSet<string>();
        [NonSerialized] List<RowEntry> _rows = new List<RowEntry>();
        [NonSerialized] List<string> _duplicateNames = new List<string>();
        [NonSerialized] UnityEngine.Object _selectedAsset; // selection anchor (survives reload/refilter)
        [NonSerialized] RowEntry _selectedRow;             // re-resolved from _selectedAsset each frame

        // Serialized UI state — survives reloads so the window restores its selection/view.
        int _typeIndex = -1;
        string _search = "";
        int _page;

        readonly DataSheetHistory _history = new DataSheetHistory();
        bool _showHistory;

        [MenuItem("Tools/Sorolla Core/DataSheet")]
        public static void Open()
        {
            var w = GetWindow<DataSheetWindow>("DataSheet");
            w.minSize = new Vector2(640, 320);
        }

        void OnEnable()
        {
            // Rebuild type caches on open and after every domain reload (System.Type fields
            // don't survive serialization). _typeIndex persists, so restore the prior selection.
            RefreshTypes();
            if (_typeIndex >= 0 && _typeIndex < _types.Count)
                SelectType(_typeIndex);
        }

        void OnFocus()
        {
            // Reload only when an asset was deleted externally. Reloading on every focus
            // would rebuild all SerializedObjects (discarding in-flight edits) and re-scan
            // the AssetDatabase on each alt-tab. Use the ↻ button to pick up renames/new assets.
            if (_selectedType == null) return;
            foreach (var row in _rows)
                if (row.asset == null) { ReloadRows(); return; }
        }

        void RefreshTypes()
        {
            _types = DataSheetModel.DiscoverTypes();
            _typeLabels = new string[_types.Count];
            for (int i = 0; i < _types.Count; i++)
                _typeLabels[i] = $"{_types[i].type.Name} ({_types[i].count})";
            if (_typeIndex >= _types.Count) _typeIndex = -1;
        }

        void SelectType(int index)
        {
            _typeIndex = index;
            _selectedType = (index >= 0 && index < _types.Count) ? _types[index].type : null;
            _page = 0;
            ClearSelection();
            if (_selectedType != null)
            {
                _allColumns = DataSheetModel.BuildColumns(_selectedType);
                LoadHiddenColumns();
            }
            else
            {
                _allColumns.Clear();
            }
            ReloadRows();
        }

        void ReloadRows()
        {
            _rows = _selectedType != null
                ? DataSheetModel.LoadRows(_selectedType, _search)
                : new List<RowEntry>();
            _duplicateNames = FindDuplicateNames(_rows);
            int maxPage = Mathf.Max(0, (_rows.Count - 1) / PageSize);
            _page = Mathf.Clamp(_page, 0, maxPage);
        }

        // Export/Import match rows by asset name, so duplicate names are ambiguous.
        static List<string> FindDuplicateNames(List<RowEntry> rows)
        {
            var seen = new HashSet<string>();
            var dups = new HashSet<string>();
            foreach (var r in rows)
                if (!seen.Add(r.name)) dups.Add(r.name);
            return new List<string>(dups);
        }

        // ---------- Column visibility (persisted per type) ----------

        void LoadHiddenColumns()
        {
            _hiddenColumns.Clear();
            string csv = EditorPrefs.GetString(HiddenColsPrefKey + _selectedType.FullName, "");
            foreach (var c in csv.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                _hiddenColumns.Add(c);
        }

        void SaveHiddenColumns() =>
            EditorPrefs.SetString(HiddenColsPrefKey + _selectedType.FullName, string.Join("|", _hiddenColumns));

        List<string> VisibleColumns()
        {
            var v = new List<string>();
            foreach (var c in _allColumns)
                if (!_hiddenColumns.Contains(c)) v.Add(c);
            return v;
        }

        // Re-resolve the selected asset to its current RowEntry (rows are rebuilt on reload).
        RowEntry ResolveSelectedRow()
        {
            if (_selectedAsset == null) return null;
            foreach (var r in _rows)
                if (r.asset == _selectedAsset) return r;
            return null;
        }

        void ClearSelection()
        {
            _selectedAsset = null;
            _selectedRow = null;
        }

        // ---------- GUI ----------

        void OnGUI()
        {
            DrawToolbar();
            if (_selectedType == null)
            {
                EditorGUILayout.HelpBox("Pick a ScriptableObject type to edit.", MessageType.Info);
                return;
            }

            if (_duplicateNames.Count > 0)
                EditorGUILayout.HelpBox(
                    $"{_duplicateNames.Count} duplicate asset name(s) (e.g. \"{_duplicateNames[0]}\"). " +
                    "Export/Import match by name — duplicates are ambiguous and may be skipped or written to the wrong asset.",
                    MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    int start = _page * PageSize;
                    int count = Mathf.Min(PageSize, _rows.Count - start);
                    var pageRows = count > 0 ? _rows.GetRange(start, count) : new List<RowEntry>();

                    _selectedRow = ResolveSelectedRow();
                    var clicked = DataSheetTable.Draw(VisibleColumns(), pageRows, _history, _selectedRow);
                    if (!ReferenceEquals(clicked, _selectedRow))
                    {
                        _selectedRow = clicked;
                        _selectedAsset = clicked?.asset;
                    }
                    DrawPager();
                }

                if (_selectedRow != null)
                    DataSheetDetailPanel.Draw(_selectedRow, _allColumns, ClearSelection);

                if (_showHistory) DrawHistoryPanel();
            }
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int newIndex = EditorGUILayout.Popup(_typeIndex, _typeLabels, EditorStyles.toolbarPopup, GUILayout.Width(200));
                if (newIndex != _typeIndex) SelectType(newIndex);

                GUILayout.Space(4);
                string newSearch = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(180));
                if (newSearch != _search) { _search = newSearch; ReloadRows(); }

                // These actions all require a selected type — disable them until one is picked.
                using (new EditorGUI.DisabledScope(_selectedType == null))
                {
                    if (GUILayout.Button("Columns ▾", EditorStyles.toolbarDropDown)) ShowColumnsMenu();
                    if (GUILayout.Button("+ Create", EditorStyles.toolbarButton)) CreateAsset();
                    if (GUILayout.Button("Export ▾", EditorStyles.toolbarDropDown)) ShowExportMenu();
                    if (GUILayout.Button("Import", EditorStyles.toolbarButton)) ImportFile();
                }

                GUILayout.FlexibleSpace();
                _showHistory = GUILayout.Toggle(_showHistory, $"History ({_history.Entries.Count})", EditorStyles.toolbarButton);
                if (GUILayout.Button("↻", EditorStyles.toolbarButton, GUILayout.Width(24))) { RefreshTypes(); ReloadRows(); }
            }
        }

        void DrawPager()
        {
            int maxPage = Mathf.Max(0, (_rows.Count - 1) / PageSize);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(_page <= 0))
                    if (GUILayout.Button("<", EditorStyles.toolbarButton, GUILayout.Width(28))) _page--;
                GUILayout.Label($"Page {_page + 1} / {maxPage + 1} · {_rows.Count} assets", EditorStyles.miniLabel);
                using (new EditorGUI.DisabledScope(_page >= maxPage))
                    if (GUILayout.Button(">", EditorStyles.toolbarButton, GUILayout.Width(28))) _page++;
                GUILayout.FlexibleSpace();
            }
        }

        void DrawHistoryPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(260)))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("History", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Clear", EditorStyles.toolbarButton)) _history.Clear();
                }
                var entries = _history.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label($"{e.timestamp}  {e.assetName}.{e.fieldPath}\n{e.oldValue} → {e.newValue}", EditorStyles.miniLabel);
                        if (GUILayout.Button("Revert", GUILayout.Width(56)))
                        {
                            RevertEntry(e, i);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        }

        // ---------- Actions ----------

        void ShowColumnsMenu()
        {
            var menu = new GenericMenu();
            foreach (var col in _allColumns)
            {
                bool visible = !_hiddenColumns.Contains(col);
                string c = col;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(col)), visible, () =>
                {
                    if (_hiddenColumns.Contains(c)) _hiddenColumns.Remove(c);
                    else _hiddenColumns.Add(c);
                    SaveHiddenColumns();
                });
            }
            menu.ShowAsContext();
        }

        void CreateAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create " + _selectedType.Name, "New " + _selectedType.Name, "asset", "");
            if (string.IsNullOrEmpty(path)) return;
            var asset = ScriptableObject.CreateInstance(_selectedType);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            ReloadRows();
        }

        SheetTable BuildCurrentTable()
        {
            var visible = VisibleColumns();
            var table = new SheetTable();
            table.Columns.Add("Name");
            table.Columns.AddRange(visible);

            foreach (var row in _rows)
            {
                row.so.Update();
                var cells = new List<string> { row.name };
                foreach (var col in visible)
                {
                    var prop = row.so.FindProperty(col);
                    if (prop == null) { cells.Add(""); continue; }
                    if (DataSheetValues.IsScalar(prop))
                        cells.Add(DataSheetValues.ReadScalar(prop));
                    else if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        cells.Add(prop.objectReferenceValue ? AssetDatabase.GetAssetPath(prop.objectReferenceValue) : "");
                    else
                        cells.Add(""); // arrays/structs: export-only skip
                }
                table.Rows.Add(cells);
            }
            return table;
        }

        void ShowExportMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("CSV"), false, () => Export("csv"));
            menu.AddItem(new GUIContent("JSON"), false, () => Export("json"));
            menu.ShowAsContext();
        }

        void Export(string ext)
        {
            string path = EditorUtility.SaveFilePanel("Export " + _selectedType.Name, "", _selectedType.Name, ext);
            if (string.IsNullOrEmpty(path)) return;
            var table = BuildCurrentTable();
            string text = ext == "csv" ? DataSheetIO.ToCsv(table) : DataSheetIO.ToJson(table);
            try
            {
                File.WriteAllText(path, text);
                EditorUtility.RevealInFinder(path);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Export failed", ex.Message, "OK");
            }
        }

        void ImportFile()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Import", "", new[] { "Table", "csv,json" });
            if (string.IsNullOrEmpty(path)) return;
            string text;
            try { text = File.ReadAllText(path); }
            catch (Exception ex) { EditorUtility.DisplayDialog("Import failed", ex.Message, "OK"); return; }

            SheetTable imported = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? DataSheetIO.ParseJson(text)
                : DataSheetIO.ParseCsv(text);

            // One-way merge: only rows whose name matches a current asset are updated.
            // Current assets absent from the imported file are left untouched (never deleted).
            var current = BuildCurrentTable();
            var diff = DataSheetIO.Diff(current, imported);

            if (diff.Changes.Count == 0)
            {
                EditorUtility.DisplayDialog("Import", "No scalar changes detected." +
                    (diff.UnmatchedRows.Count > 0 ? $"\n{diff.UnmatchedRows.Count} unmatched row(s) ignored." : ""), "OK");
                return;
            }

            string preview = $"{diff.Changes.Count} cell change(s) will be applied.";
            if (diff.UnmatchedRows.Count > 0)
                preview += $"\n{diff.UnmatchedRows.Count} unmatched row(s) ignored.";
            if (_duplicateNames.Count > 0)
                preview += $"\n\nWARNING: {_duplicateNames.Count} duplicate asset name(s) — those rows may be applied to the wrong asset.";
            preview += "\n\nObject references and arrays are not imported.";
            if (!EditorUtility.DisplayDialog("Apply Import?", preview, "Apply", "Cancel")) return;

            ApplyDiff(diff);
        }

        void ApplyDiff(ImportDiff diff)
        {
            var byName = new Dictionary<string, RowEntry>();
            foreach (var r in _rows) byName[r.name] = r;

            int applied = 0, skipped = 0;
            foreach (var ch in diff.Changes)
            {
                if (!byName.TryGetValue(ch.rowKey, out var row)) { skipped++; continue; }
                var prop = row.so.FindProperty(ch.column);
                if (prop == null || !DataSheetValues.IsScalar(prop)) { skipped++; continue; }

                row.so.Update();
                string before = DataSheetValues.ReadScalar(prop);
                if (!DataSheetValues.WriteScalar(prop, ch.newValue)) { skipped++; continue; }

                Undo.RecordObject(row.asset, "DataSheet Import");
                row.so.ApplyModifiedProperties();
                _history.Record(new ChangeEntry
                {
                    assetName = row.name,
                    fieldPath = ch.column,
                    oldValue = before,
                    newValue = ch.newValue,
                    timestamp = DateTime.Now.ToString("HH:mm:ss")
                });
                applied++;
            }
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Import", $"Applied {applied} change(s); skipped {skipped}.", "OK");
        }

        void RevertEntry(ChangeEntry e, int index)
        {
            foreach (var row in _rows)
            {
                if (row.name != e.assetName) continue;
                var prop = row.so.FindProperty(e.fieldPath);
                if (prop == null || !DataSheetValues.IsScalar(prop)) break;
                row.so.Update();
                Undo.RecordObject(row.asset, "DataSheet Revert");
                if (DataSheetValues.WriteScalar(prop, e.oldValue))
                {
                    row.so.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                }
                break;
            }
            _history.RemoveAt(index);
        }
    }
}
