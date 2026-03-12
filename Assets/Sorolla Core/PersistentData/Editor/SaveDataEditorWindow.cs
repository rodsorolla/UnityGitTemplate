using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Sorolla.PersistentData.Editor
{
    /// <summary>
    /// Editor window for viewing and editing save data during development.
    /// WARNING: This is for TESTING ONLY. Changes are marked with [EDITOR MODIFIED].
    /// </summary>
    public class SaveDataEditorWindow : EditorWindow
    {
        private const string EditorModifiedMarker = "__editorModified";
        private const string MetadataFileName = "editor_metadata.json";

        private Vector2 _fileListScroll;
        private Vector2 _contentScroll;
        private string[] _saveFiles = Array.Empty<string>();
        private int _selectedSlot;
        private int _selectedFileIndex = -1;
        private string _selectedFileName;
        private string _originalJson;
        private string _editedJson;
        private bool _isDirty;
        private bool _showRawJson;
        private string _savesBasePath;
        private HashSet<string> _editorModifiedFiles = new();

        [MenuItem("Tools/Sorolla/Save Data Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveDataEditorWindow>("Save Data Editor");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        [MenuItem("Tools/Sorolla/Delete All Saves %#r")]
        public static void DeleteAllSavesShortcut()
        {
            var savesBasePath = Path.Combine(Application.persistentDataPath, "saves");

            if (!Directory.Exists(savesBasePath))
            {
                EditorUtility.DisplayDialog("No Saves", "No save folder found.", "OK");
                return;
            }

            var files = Directory.GetFiles(savesBasePath, "*.json", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog("No Saves", "No save files found.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete All Saves",
                $"Are you sure you want to DELETE ALL {files.Length} save file(s)?\n\n⚠️ THIS CANNOT BE UNDONE ⚠️",
                "Delete All", "Cancel"))
                return;

            try
            {
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                Debug.Log($"[SaveDataEditor] Deleted all {files.Length} save files");
                EditorUtility.DisplayDialog("Done", $"Deleted {files.Length} save file(s).", "OK");

                // Refresh the editor window if it's open
                if (HasOpenInstances<SaveDataEditorWindow>())
                {
                    var window = GetWindow<SaveDataEditorWindow>();
                    window._editorModifiedFiles.Clear();
                    window.SaveMetadata();
                    window.RefreshFileList();
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to delete saves: {ex.Message}", "OK");
            }
        }

        private void OnEnable()
        {
            _savesBasePath = Path.Combine(Application.persistentDataPath, "saves");
            LoadMetadata();
            RefreshFileList();
        }

        private void OnGUI()
        {
            DrawWarningHeader();

            EditorGUILayout.BeginHorizontal();
            {
                DrawFileListPanel();
                DrawContentPanel();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawWarningHeader()
        {
            var warningStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            EditorGUILayout.BeginVertical(warningStyle, GUILayout.Height(40));
            EditorGUILayout.LabelField("⚠️ TESTING ONLY - Changes are marked [EDITOR MODIFIED] ⚠️", warningStyle);
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);
        }

        private void DrawFileListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            {
                EditorGUILayout.LabelField("Save Files", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Slot:", GUILayout.Width(35));
                var newSlot = EditorGUILayout.IntField(_selectedSlot, GUILayout.Width(40));
                if (newSlot != _selectedSlot)
                {
                    _selectedSlot = Mathf.Max(0, newSlot);
                    RefreshFileList();
                }
                if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                    RefreshFileList();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                _fileListScroll = EditorGUILayout.BeginScrollView(_fileListScroll, EditorStyles.helpBox);
                {
                    for (int i = 0; i < _saveFiles.Length; i++)
                    {
                        var fileName = _saveFiles[i];
                        var isModified = _editorModifiedFiles.Contains(GetFullPath(fileName));
                        var displayName = isModified ? $"[M] {fileName}" : fileName;

                        var style = i == _selectedFileIndex
                            ? new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, normal = { textColor = Color.cyan } }
                            : EditorStyles.label;

                        if (isModified)
                            style.normal.textColor = new Color(1f, 0.6f, 0.2f);

                        if (GUILayout.Button(displayName, style))
                        {
                            if (_isDirty && !ConfirmDiscardChanges())
                                continue;

                            SelectFile(i);
                        }
                    }

                    if (_saveFiles.Length == 0)
                        EditorGUILayout.LabelField("No save files found", EditorStyles.centeredGreyMiniLabel);
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(5);

                if (GUILayout.Button("Open Saves Folder"))
                {
                    // Create folder if it doesn't exist, otherwise RevealInFinder does nothing
                    if (!Directory.Exists(_savesBasePath))
                        Directory.CreateDirectory(_savesBasePath);
                    EditorUtility.RevealInFinder(_savesBasePath);
                }

                GUI.backgroundColor = new Color(1f, 0.8f, 0.5f);
                if (GUILayout.Button("Remove Editor Markers"))
                    CleanAllEditorModifiedSaves();
                GUI.backgroundColor = Color.white;

                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUILayout.Button("Delete All Saves"))
                    DeleteAllSaves();
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawContentPanel()
        {
            EditorGUILayout.BeginVertical();
            {
                if (string.IsNullOrEmpty(_selectedFileName))
                {
                    EditorGUILayout.HelpBox("Select a save file to view its contents.", MessageType.Info);
                }
                else
                {
                    DrawContentHeader();
                    DrawContentBody();
                    DrawContentFooter();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawContentHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                EditorGUILayout.LabelField(_selectedFileName, EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                if (_isDirty)
                {
                    GUI.backgroundColor = Color.yellow;
                    EditorGUILayout.LabelField("● Unsaved Changes", GUILayout.Width(120));
                    GUI.backgroundColor = Color.white;
                }

                _showRawJson = GUILayout.Toggle(_showRawJson, "Raw JSON", EditorStyles.toolbarButton, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawContentBody()
        {
            _contentScroll = EditorGUILayout.BeginScrollView(_contentScroll, EditorStyles.helpBox);
            {
                if (_showRawJson)
                {
                    var newJson = EditorGUILayout.TextArea(_editedJson, GUILayout.ExpandHeight(true));
                    if (newJson != _editedJson)
                    {
                        _editedJson = newJson;
                        _isDirty = _editedJson != _originalJson;
                    }
                }
                else
                {
                    DrawJsonTree();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawJsonTree()
        {
            try
            {
                var obj = JObject.Parse(_editedJson);
                var changed = DrawJToken(obj, "");

                if (changed)
                {
                    _editedJson = obj.ToString(Formatting.Indented);
                    _isDirty = _editedJson != _originalJson;
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"Invalid JSON: {ex.Message}", MessageType.Error);
            }
        }

        private bool DrawJToken(JToken token, string path)
        {
            var changed = false;

            switch (token)
            {
                case JObject obj:
                    foreach (var prop in obj.Properties())
                    {
                        var propPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";

                        if (prop.Value is JObject nestedObj)
                        {
                            EditorGUILayout.LabelField(prop.Name, EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;

                            // Check if this looks like a dictionary (object with primitive values)
                            if (IsDictionaryLikeObject(nestedObj))
                            {
                                changed |= DrawDictionaryObject(nestedObj, propPath);
                            }
                            else
                            {
                                changed |= DrawJToken(nestedObj, propPath);
                            }

                            EditorGUI.indentLevel--;
                        }
                        else if (prop.Value is JArray)
                        {
                            EditorGUILayout.LabelField(prop.Name, EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;
                            changed |= DrawJToken(prop.Value, propPath);
                            EditorGUI.indentLevel--;
                        }
                        else
                        {
                            changed |= DrawValueField(obj, prop.Name, prop.Value);
                        }
                    }
                    break;

                case JArray arr:
                    changed |= DrawArrayItems(arr, path);
                    break;
            }

            return changed;
        }

        private bool IsDictionaryLikeObject(JObject obj)
        {
            if (!obj.HasValues) return true; // Empty object can be treated as dictionary

            JTokenType? firstType = null;
            foreach (var prop in obj.Properties())
            {
                // Skip metadata properties
                if (prop.Name.StartsWith("__")) continue;

                // Dictionary values should be primitives
                if (prop.Value is JObject || prop.Value is JArray)
                    return false;

                if (firstType == null)
                    firstType = prop.Value.Type;
                else if (prop.Value.Type != firstType)
                    return false; // Mixed types = not a dictionary
            }
            return true;
        }

        private bool DrawDictionaryObject(JObject obj, string path)
        {
            var changed = false;
            string keyToRemove = null;

            foreach (var prop in obj.Properties())
            {
                // Skip metadata properties
                if (prop.Name.StartsWith("__")) continue;

                EditorGUILayout.BeginHorizontal();

                // Key (read-only for now, shown as label)
                EditorGUILayout.LabelField(prop.Name, GUILayout.Width(120));

                // Value (editable)
                switch (prop.Value.Type)
                {
                    case JTokenType.Integer:
                        var intVal = EditorGUILayout.IntField(prop.Value.Value<int>());
                        if (intVal != prop.Value.Value<int>())
                        {
                            obj[prop.Name] = intVal;
                            changed = true;
                        }
                        break;

                    case JTokenType.String:
                        var strVal = EditorGUILayout.TextField(prop.Value.Value<string>());
                        if (strVal != prop.Value.Value<string>())
                        {
                            obj[prop.Name] = strVal;
                            changed = true;
                        }
                        break;

                    case JTokenType.Boolean:
                        var boolVal = EditorGUILayout.Toggle(prop.Value.Value<bool>());
                        if (boolVal != prop.Value.Value<bool>())
                        {
                            obj[prop.Name] = boolVal;
                            changed = true;
                        }
                        break;

                    case JTokenType.Float:
                        var floatVal = EditorGUILayout.FloatField(prop.Value.Value<float>());
                        if (!Mathf.Approximately(floatVal, prop.Value.Value<float>()))
                        {
                            obj[prop.Name] = floatVal;
                            changed = true;
                        }
                        break;

                    default:
                        EditorGUILayout.LabelField(prop.Value.ToString());
                        break;
                }

                // Remove button
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    keyToRemove = prop.Name;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            // Handle removal after iteration
            if (keyToRemove != null)
            {
                obj.Remove(keyToRemove);
                changed = true;
            }

            // Add new entry button
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("+ Add Entry", GUILayout.Width(100)))
            {
                // Generate unique key
                var newKey = "new_key";
                int suffix = 1;
                while (obj.ContainsKey(newKey))
                {
                    newKey = $"new_key_{suffix++}";
                }

                // Determine value type from existing entries, default to int
                JTokenType valueType = JTokenType.Integer;
                foreach (var prop in obj.Properties())
                {
                    if (!prop.Name.StartsWith("__"))
                    {
                        valueType = prop.Value.Type;
                        break;
                    }
                }

                switch (valueType)
                {
                    case JTokenType.String:
                        obj[newKey] = "";
                        break;
                    case JTokenType.Boolean:
                        obj[newKey] = false;
                        break;
                    case JTokenType.Float:
                        obj[newKey] = 0f;
                        break;
                    default:
                        obj[newKey] = 0;
                        break;
                }
                changed = true;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            return changed;
        }

        private bool DrawArrayItems(JArray arr, string path)
        {
            var changed = false;
            int? indexToRemove = null;

            for (int i = 0; i < arr.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var item = arr[i];

                // Handle primitive values in arrays (strings, ints, etc.)
                if (item.Type == JTokenType.String)
                {
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                    var strVal = EditorGUILayout.TextField(item.Value<string>());
                    if (strVal != item.Value<string>())
                    {
                        arr[i] = strVal;
                        changed = true;
                    }
                }
                else if (item.Type == JTokenType.Integer)
                {
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                    var intVal = EditorGUILayout.IntField(item.Value<int>());
                    if (intVal != item.Value<int>())
                    {
                        arr[i] = intVal;
                        changed = true;
                    }
                }
                else if (item.Type == JTokenType.Float)
                {
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                    var floatVal = EditorGUILayout.FloatField(item.Value<float>());
                    if (!Mathf.Approximately(floatVal, item.Value<float>()))
                    {
                        arr[i] = floatVal;
                        changed = true;
                    }
                }
                else if (item.Type == JTokenType.Boolean)
                {
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                    var boolVal = EditorGUILayout.Toggle(item.Value<bool>());
                    if (boolVal != item.Value<bool>())
                    {
                        arr[i] = boolVal;
                        changed = true;
                    }
                }
                else
                {
                    // Complex objects - show label and recurse
                    EditorGUILayout.LabelField($"[{i}]", EditorStyles.miniLabel);
                }

                // Remove button
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    indexToRemove = i;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                // For complex objects, draw nested content
                if (item is JObject || item is JArray)
                {
                    EditorGUI.indentLevel++;
                    changed |= DrawJToken(item, $"{path}[{i}]");
                    EditorGUI.indentLevel--;
                }
            }

            // Handle removal after iteration
            if (indexToRemove.HasValue)
            {
                arr.RemoveAt(indexToRemove.Value);
                changed = true;
            }

            // Add button
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("+ Add Item", GUILayout.Width(100)))
            {
                // Determine type from existing items, default to string
                if (arr.Count > 0)
                {
                    var firstType = arr[0].Type;
                    switch (firstType)
                    {
                        case JTokenType.String:
                            arr.Add("");
                            break;
                        case JTokenType.Integer:
                            arr.Add(0);
                            break;
                        case JTokenType.Float:
                            arr.Add(0f);
                            break;
                        case JTokenType.Boolean:
                            arr.Add(false);
                            break;
                        default:
                            arr.Add(new JObject());
                            break;
                    }
                }
                else
                {
                    arr.Add(""); // Default to string for empty arrays
                }
                changed = true;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            return changed;
        }

        private bool DrawValueField(JObject parent, string name, JToken value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name, GUILayout.Width(150));

            var changed = false;

            switch (value.Type)
            {
                case JTokenType.Integer:
                    var intVal = EditorGUILayout.IntField(value.Value<int>());
                    if (intVal != value.Value<int>())
                    {
                        parent[name] = intVal;
                        changed = true;
                    }
                    break;

                case JTokenType.Float:
                    var floatVal = EditorGUILayout.FloatField(value.Value<float>());
                    if (!Mathf.Approximately(floatVal, value.Value<float>()))
                    {
                        parent[name] = floatVal;
                        changed = true;
                    }
                    break;

                case JTokenType.Boolean:
                    var boolVal = EditorGUILayout.Toggle(value.Value<bool>());
                    if (boolVal != value.Value<bool>())
                    {
                        parent[name] = boolVal;
                        changed = true;
                    }
                    break;

                case JTokenType.String:
                    var strVal = EditorGUILayout.TextField(value.Value<string>());
                    if (strVal != value.Value<string>())
                    {
                        parent[name] = strVal;
                        changed = true;
                    }
                    break;

                default:
                    EditorGUILayout.LabelField(value.ToString(), EditorStyles.wordWrappedLabel);
                    break;
            }

            EditorGUILayout.EndHorizontal();
            return changed;
        }

        private void DrawContentFooter()
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            {
                GUI.enabled = _isDirty;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Apply Changes", GUILayout.Height(30)))
                    ApplyChanges();
                GUI.backgroundColor = Color.white;

                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Revert", GUILayout.Height(30)))
                    RevertChanges();
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;

                GUILayout.FlexibleSpace();

                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("Delete Save", GUILayout.Height(30), GUILayout.Width(100)))
                    DeleteSelectedSave();
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshFileList()
        {
            var slotFolder = _selectedSlot == 0 ? "default" : $"slot{_selectedSlot}";
            var slotPath = Path.Combine(_savesBasePath, slotFolder);

            if (Directory.Exists(slotPath))
            {
                var files = Directory.GetFiles(slotPath, "*.json");
                _saveFiles = new string[files.Length];
                for (int i = 0; i < files.Length; i++)
                    _saveFiles[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            else
            {
                _saveFiles = Array.Empty<string>();
            }

            _selectedFileIndex = -1;
            _selectedFileName = null;
            _originalJson = null;
            _editedJson = null;
            _isDirty = false;
        }

        private void SelectFile(int index)
        {
            _selectedFileIndex = index;
            _selectedFileName = _saveFiles[index];

            var filePath = GetFullPath(_selectedFileName);

            if (File.Exists(filePath))
            {
                _originalJson = File.ReadAllText(filePath);
                try
                {
                    var obj = JObject.Parse(_originalJson);
                    _editedJson = obj.ToString(Formatting.Indented);
                    _originalJson = _editedJson;
                }
                catch
                {
                    _editedJson = _originalJson;
                }
            }
            else
            {
                _originalJson = "";
                _editedJson = "";
            }

            _isDirty = false;
        }

        private void ApplyChanges()
        {
            if (!SaveValidator.IsValidJson(_editedJson))
            {
                EditorUtility.DisplayDialog("Invalid JSON", "Cannot save: JSON is invalid.", "OK");
                return;
            }

            var filePath = GetFullPath(_selectedFileName);

            // Show diff
            var message = $"Apply changes to {_selectedFileName}?\n\nThis will mark the file as [EDITOR MODIFIED].";
            if (!EditorUtility.DisplayDialog("Confirm Changes", message, "Apply", "Cancel"))
                return;

            try
            {
                // Add editor modified marker
                var obj = JObject.Parse(_editedJson);
                obj[EditorModifiedMarker] = DateTime.Now.ToString("o");
                var finalJson = obj.ToString(Formatting.Indented);

                File.WriteAllText(filePath, finalJson);

                _editorModifiedFiles.Add(filePath);
                SaveMetadata();

                _originalJson = finalJson;
                _editedJson = finalJson;
                _isDirty = false;

                Debug.Log($"[SaveDataEditor] Applied changes to {_selectedFileName}");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to save: {ex.Message}", "OK");
            }
        }

        private void RevertChanges()
        {
            _editedJson = _originalJson;
            _isDirty = false;
        }

        private void DeleteSelectedSave()
        {
            if (!EditorUtility.DisplayDialog("Delete Save",
                $"Are you sure you want to delete '{_selectedFileName}'?\n\nThis cannot be undone.",
                "Delete", "Cancel"))
                return;

            var filePath = GetFullPath(_selectedFileName);

            try
            {
                File.Delete(filePath);
                _editorModifiedFiles.Remove(filePath);
                SaveMetadata();

                Debug.Log($"[SaveDataEditor] Deleted {_selectedFileName}");
                RefreshFileList();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to delete: {ex.Message}", "OK");
            }
        }

        private void CleanAllEditorModifiedSaves()
        {
            if (_editorModifiedFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("No Modified Files", "No editor-modified save files found.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Clean Editor Saves",
                $"Remove editor marker from {_editorModifiedFiles.Count} file(s)?\n\nThis will remove the [EDITOR MODIFIED] marker but keep the data.",
                "Clean", "Cancel"))
                return;

            foreach (var filePath in _editorModifiedFiles)
            {
                try
                {
                    if (!File.Exists(filePath))
                        continue;

                    var json = File.ReadAllText(filePath);
                    var obj = JObject.Parse(json);
                    obj.Remove(EditorModifiedMarker);
                    File.WriteAllText(filePath, obj.ToString(Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveDataEditor] Failed to clean {filePath}: {ex.Message}");
                }
            }

            _editorModifiedFiles.Clear();
            SaveMetadata();
            RefreshFileList();

            Debug.Log("[SaveDataEditor] Cleaned all editor-modified saves");
        }

        private void DeleteAllSaves()
        {
            if (!Directory.Exists(_savesBasePath))
            {
                EditorUtility.DisplayDialog("No Saves", "No save folder found.", "OK");
                return;
            }

            var files = Directory.GetFiles(_savesBasePath, "*.json", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog("No Saves", "No save files found.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete All Saves",
                $"Are you sure you want to DELETE ALL {files.Length} save file(s)?\n\n⚠️ THIS CANNOT BE UNDONE ⚠️",
                "Delete All", "Cancel"))
                return;

            try
            {
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                _editorModifiedFiles.Clear();
                SaveMetadata();
                RefreshFileList();

                Debug.Log($"[SaveDataEditor] Deleted all {files.Length} save files");
                EditorUtility.DisplayDialog("Done", $"Deleted {files.Length} save file(s).", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to delete saves: {ex.Message}", "OK");
            }
        }

        private bool ConfirmDiscardChanges()
        {
            return EditorUtility.DisplayDialog("Unsaved Changes",
                "You have unsaved changes. Discard them?",
                "Discard", "Cancel");
        }

        private string GetFullPath(string fileName)
        {
            var slotFolder = _selectedSlot == 0 ? "default" : $"slot{_selectedSlot}";
            return Path.Combine(_savesBasePath, slotFolder, fileName + ".json");
        }

        private void LoadMetadata()
        {
            var metadataPath = Path.Combine(_savesBasePath, MetadataFileName);

            if (!File.Exists(metadataPath))
                return;

            try
            {
                var json = File.ReadAllText(metadataPath);
                var files = JsonConvert.DeserializeObject<List<string>>(json);
                _editorModifiedFiles = new HashSet<string>(files ?? new List<string>());
            }
            catch
            {
                _editorModifiedFiles = new HashSet<string>();
            }
        }

        private void SaveMetadata()
        {
            var metadataPath = Path.Combine(_savesBasePath, MetadataFileName);

            try
            {
                var directory = Path.GetDirectoryName(metadataPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonConvert.SerializeObject(new List<string>(_editorModifiedFiles), Formatting.Indented);
                File.WriteAllText(metadataPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveDataEditor] Failed to save metadata: {ex.Message}");
            }
        }
    }
}
