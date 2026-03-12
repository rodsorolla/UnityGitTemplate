using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;

namespace Sorolla.GoogleSheets
{
    /// <summary>
    /// Abstract base for Google Sheets importer editor windows.
    /// Handles: config loading, OnGUI layout, CSV fetching, status display, folder creation.
    /// Game-specific importers implement the abstract members.
    /// </summary>
    public abstract class GoogleSheetsImporterBase : EditorWindow
    {
        private GoogleSheetsConfigBase _config;
        private Vector2 _scrollPosition;
        private string _statusMessage = "Ready";
        private MessageType _statusType = MessageType.Info;
        private bool _isImporting;
        private int _pendingRequests;
        private Dictionary<string, string> _fetchedData;

        #region Abstract Members

        /// <summary>Type name for AssetDatabase.FindAssets (e.g., "GoogleSheetsConfig").</summary>
        protected abstract string ConfigTypeName { get; }

        protected abstract string GetWindowTitle();
        protected abstract string GetHelpText();

        /// <summary>Draw game-specific URL TextFields in OnGUI.</summary>
        protected abstract void DrawSheetUrlFields();

        /// <summary>Whether all required URLs are configured.</summary>
        protected abstract bool HasValidUrls();

        /// <summary>Returns name-to-URL pairs for all sheets to fetch.</summary>
        protected abstract Dictionary<string, string> GetSheetUrls();

        /// <summary>Process fetched CSV data. Keys match those from GetSheetUrls().</summary>
        protected abstract void ProcessSheets(Dictionary<string, string> csvData);

        /// <summary>Create a new concrete config asset at the given folder path.</summary>
        protected abstract GoogleSheetsConfigBase CreateNewConfig(string folderPath);

        #endregion

        /// <summary>The current config asset. Subclass casts to its concrete type.</summary>
        protected GoogleSheetsConfigBase Config => _config;

        private void OnEnable()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{ConfigTypeName}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _config = AssetDatabase.LoadAssetAtPath<GoogleSheetsConfigBase>(path);
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(GetWindowTitle(), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(GetHelpText(), MessageType.Info);

            EditorGUILayout.Space(10);

            // Config field
            EditorGUI.BeginChangeCheck();
            _config = (GoogleSheetsConfigBase)EditorGUILayout.ObjectField(
                "Config", _config, typeof(GoogleSheetsConfigBase), false);
            if (EditorGUI.EndChangeCheck() && _config != null)
            {
                EditorUtility.SetDirty(_config);
            }

            // Create config button if none exists
            if (_config == null)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Create New Config"))
                {
                    HandleCreateNewConfig();
                }
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space(10);

            // Game-specific URL fields
            DrawSheetUrlFields();

            // Output folder
            EditorGUI.BeginChangeCheck();
            _config.OutputFolder = EditorGUILayout.TextField("Output Folder", _config.OutputFolder);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_config);
            }

            EditorGUILayout.Space(20);

            // Import button
            EditorGUI.BeginDisabledGroup(_isImporting || !HasValidUrls());
            if (GUILayout.Button("Import from Google Sheets", GUILayout.Height(40)))
            {
                StartImport();
            }
            EditorGUI.EndDisabledGroup();

            if (!HasValidUrls())
            {
                EditorGUILayout.HelpBox("Please configure all required sheet URLs", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Status
            EditorGUILayout.HelpBox(_statusMessage, _statusType);

            EditorGUILayout.EndScrollView();
        }

        private void HandleCreateNewConfig()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select folder for config", "Assets", "");
            if (string.IsNullOrEmpty(folderPath)) return;

            // Convert absolute path to Assets-relative
            if (folderPath.StartsWith(Application.dataPath))
            {
                folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);
            }

            _config = CreateNewConfig(folderPath);
            if (_config != null)
            {
                SetStatus("Created new config asset", MessageType.Info);
            }
        }

        private void StartImport()
        {
            var urls = GetSheetUrls();
            _isImporting = true;
            _pendingRequests = urls.Count;
            _fetchedData = new Dictionary<string, string>();

            SetStatus("Fetching data from Google Sheets...", MessageType.Info);

            foreach (var kvp in urls)
            {
                string sheetName = kvp.Key;
                FetchSheet(kvp.Value, csv =>
                {
                    _fetchedData[sheetName] = csv;
                });
            }
        }

        private void FetchSheet(string url, Action<string> onComplete)
        {
            var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();

            operation.completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    onComplete(request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"[GoogleSheetsImporter] Failed to fetch {url}: {request.error}");
                    onComplete(null);
                }
                request.Dispose();

                _pendingRequests--;
                if (_pendingRequests <= 0)
                {
                    OnAllSheetsFetched();
                }

                Repaint();
            };
        }

        private void OnAllSheetsFetched()
        {
            // Check if any fetch failed
            foreach (var kvp in _fetchedData)
            {
                if (string.IsNullOrEmpty(kvp.Value))
                {
                    SetStatus("Failed to fetch one or more sheets. Check console for errors.", MessageType.Error);
                    _isImporting = false;
                    return;
                }
            }

            try
            {
                ProcessSheets(_fetchedData);
                SetStatus("Successfully imported data!", MessageType.Info);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GoogleSheetsImporter] Error processing sheets: {e}");
                SetStatus($"Error: {e.Message}", MessageType.Error);
            }

            _isImporting = false;
        }

        protected void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }

        /// <summary>
        /// Ensures a folder path exists in the AssetDatabase, creating intermediate folders as needed.
        /// </summary>
        protected static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
