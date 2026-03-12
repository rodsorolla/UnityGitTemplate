using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// EditorWindow for browsing prefabs, previewing icon renders, and batch-exporting PNG icons.
    /// Three-panel layout: prefab list | thumbnail grid | settings + preview.
    /// Panel widths are adjustable via draggable splitters.
    /// </summary>
    public class PrefabIconGeneratorWindow : EditorWindow
    {
        private struct PrefabEntry
        {
            public string Guid;
            public string Path;
            public string Name;
        }

        // EditorPrefs prefix
        private const string PrefsPrefix = "Sorolla.IconGen.";

        // Splitter
        private const float SplitterWidth = 4f;
        private const float MinLeftWidth = 120f;
        private const float MaxLeftWidth = 400f;
        private const float MinRightWidth = 250f;
        private const float MaxRightWidth = 500f;
        private float _leftPanelWidth = 200f;
        private float _rightPanelWidth = 300f;
        private int _draggingSplitter; // 0=none, 1=left, 2=right

        // State
        private string _sourceFolder = "Assets";
        private string _outputFolder = "Assets";
        private List<PrefabEntry> _prefabs = new();
        private int _previewPrefabIndex = -1;
        private HashSet<string> _selectedGuids = new();
        private int _lastClickedIndex = -1;

        // Thumbnails — only caches exported PNGs; Unity asset previews are not cached
        private Dictionary<string, Texture2D> _thumbnailCache = new();
        private int _thumbnailSize = 96;

        // Preview
        private Texture2D _previewTexture;
        private bool _previewDirty;
        private double _previewDirtyTime;
        private const double PreviewDebounce = 0.05;

        // Settings
        private IconRenderSettings _settings;

        // Foldouts
        private bool _cameraFoldout = true;
        private bool _lightFoldout = true;
        private bool _exportFoldout = true;

        // Scroll positions
        private Vector2 _leftScroll;
        private Vector2 _centerScroll;
        private Vector2 _rightScroll;

        // Resolution/supersample popup options
        private static readonly int[] Resolutions = { 128, 256, 512, 1024, 2048 };
        private static readonly string[] ResolutionLabels = { "128", "256", "512", "1024", "2048" };
        private static readonly int[] SupersampleFactors = { 1, 2, 4 };
        private static readonly string[] SupersampleLabels = { "1x (None)", "2x", "4x" };

        [MenuItem("Tools/Sorolla/Prefab Icon Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabIconGeneratorWindow>("Prefab Icon Generator");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
            RefreshPrefabList();
        }

        private void OnDisable()
        {
            SaveSettings();
            CleanupPreview();
        }

        private void Update()
        {
            // Debounced preview rendering
            if (_previewDirty && EditorApplication.timeSinceStartup > _previewDirtyTime + PreviewDebounce)
            {
                _previewDirty = false;
                RenderPreview();
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                DrawLeftPanel();
                DrawSplitter(ref _leftPanelWidth, 1, MinLeftWidth, MaxLeftWidth, false);
                DrawCenterPanel();
                DrawSplitter(ref _rightPanelWidth, 2, MinRightWidth, MaxRightWidth, true);
                DrawRightPanel();
            }
            EditorGUILayout.EndHorizontal();
        }

        // ─── Draggable Splitter ───────────────────────────────────

        private void DrawSplitter(ref float panelWidth, int id, float min, float max, bool invertDrag)
        {
            var rect = GUILayoutUtility.GetRect(SplitterWidth, SplitterWidth,
                GUILayout.Width(SplitterWidth), GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);

            // Subtle divider line
            EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y, 1, rect.height), new Color(0, 0, 0, 0.3f));

            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown when rect.Contains(e.mousePosition):
                    _draggingSplitter = id;
                    e.Use();
                    break;
                case EventType.MouseDrag when _draggingSplitter == id:
                    panelWidth += invertDrag ? -e.delta.x : e.delta.x;
                    panelWidth = Mathf.Clamp(panelWidth, min, max);
                    Repaint();
                    e.Use();
                    break;
                case EventType.MouseUp when _draggingSplitter == id:
                    _draggingSplitter = 0;
                    SaveSettings();
                    e.Use();
                    break;
            }
        }

        // ─── Left Panel: Source Folder + Prefab List ──────────────

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_leftPanelWidth));
            {
                EditorGUILayout.LabelField("Source Folder", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(_sourceFolder, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("...", GUILayout.Width(30)))
                    {
                        string folder = EditorUtility.OpenFolderPanel("Select Prefab Folder", _sourceFolder, "");
                        if (!string.IsNullOrEmpty(folder))
                        {
                            string dataPath = Application.dataPath;
                            _sourceFolder = folder.StartsWith(dataPath)
                                ? "Assets" + folder.Substring(dataPath.Length)
                                : folder;
                            RefreshPrefabList();
                            SaveSettings();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Refresh"))
                        RefreshPrefabList();
                    EditorGUILayout.LabelField($"{_prefabs.Count} prefabs", EditorStyles.centeredGreyMiniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, EditorStyles.helpBox);
                {
                    for (int i = 0; i < _prefabs.Count; i++)
                    {
                        bool isCurrent = i == _previewPrefabIndex;
                        var style = isCurrent
                            ? new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, normal = { textColor = Color.cyan } }
                            : EditorStyles.label;

                        if (GUILayout.Button(_prefabs[i].Name, style))
                        {
                            _previewPrefabIndex = i;
                            MarkPreviewDirty();
                        }
                    }

                    if (_prefabs.Count == 0)
                        EditorGUILayout.LabelField("No prefabs found", EditorStyles.centeredGreyMiniLabel);
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        // ─── Center Panel: Toolbar + Thumbnail Grid ───────────────

        private void DrawCenterPanel()
        {
            EditorGUILayout.BeginVertical();
            {
                // Toolbar
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    if (GUILayout.Button("Select All", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    {
                        _selectedGuids.Clear();
                        foreach (var p in _prefabs) _selectedGuids.Add(p.Guid);
                    }
                    if (GUILayout.Button("Deselect", EditorStyles.toolbarButton, GUILayout.Width(60)))
                        _selectedGuids.Clear();

                    GUILayout.FlexibleSpace();

                    EditorGUILayout.LabelField("Size:", GUILayout.Width(32));
                    _thumbnailSize = (int)GUILayout.HorizontalSlider(_thumbnailSize, 48, 160, GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{_thumbnailSize}px", GUILayout.Width(40));
                }
                EditorGUILayout.EndHorizontal();

                // Thumbnail grid
                _centerScroll = EditorGUILayout.BeginScrollView(_centerScroll, EditorStyles.helpBox);
                {
                    DrawThumbnailGrid();
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawThumbnailGrid()
        {
            if (_prefabs.Count == 0)
            {
                EditorGUILayout.LabelField("No prefabs loaded", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // Available width = total minus left/right panels, splitters, scrollbar padding
            float availableWidth = position.width - _leftPanelWidth - _rightPanelWidth - SplitterWidth * 2 - 30;
            int cellSize = _thumbnailSize + 10;
            int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / cellSize));

            int col = 0;
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < _prefabs.Count; i++)
            {
                if (col >= columns)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    col = 0;
                }

                DrawThumbnailCell(i);
                col++;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawThumbnailCell(int index)
        {
            var entry = _prefabs[index];
            bool isSelected = _selectedGuids.Contains(entry.Guid);

            EditorGUILayout.BeginVertical(GUILayout.Width(_thumbnailSize + 4));
            {
                // Highlight selected cells
                if (isSelected)
                    GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);

                var tex = GetThumbnail(entry.Guid, entry.Path);
                var rect = GUILayoutUtility.GetRect(_thumbnailSize, _thumbnailSize, GUILayout.Width(_thumbnailSize));

                if (tex != null)
                {
                    if (GUI.Button(rect, tex))
                        HandleThumbnailClick(index);
                }
                else
                {
                    if (GUI.Button(rect, entry.Name))
                        HandleThumbnailClick(index);
                }

                GUI.backgroundColor = Color.white;

                // Name label
                var labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    wordWrap = true,
                    fixedHeight = 0
                };
                EditorGUILayout.LabelField(entry.Name, labelStyle, GUILayout.Width(_thumbnailSize));
            }
            EditorGUILayout.EndVertical();
        }

        private void HandleThumbnailClick(int index)
        {
            Event e = Event.current;
            string guid = _prefabs[index].Guid;

            if (e.control || e.command)
            {
                // Ctrl/Cmd+Click: toggle selection
                if (!_selectedGuids.Remove(guid))
                    _selectedGuids.Add(guid);
            }
            else if (e.shift && _lastClickedIndex >= 0)
            {
                // Shift+Click: range select
                int from = Mathf.Min(_lastClickedIndex, index);
                int to = Mathf.Max(_lastClickedIndex, index);
                for (int i = from; i <= to; i++)
                    _selectedGuids.Add(_prefabs[i].Guid);
            }
            else
            {
                // Normal click: single select
                _selectedGuids.Clear();
                _selectedGuids.Add(guid);
            }

            _lastClickedIndex = index;

            // Also update preview
            _previewPrefabIndex = index;
            MarkPreviewDirty();
        }

        // ─── Right Panel: Preview + Settings + Export ─────────────

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_rightPanelWidth));
            {
                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
                {
                    DrawPreview();
                    EditorGUILayout.Space(5);

                    _cameraFoldout = EditorGUILayout.Foldout(_cameraFoldout, "Camera Settings", true);
                    if (_cameraFoldout) DrawCameraSettings();

                    _lightFoldout = EditorGUILayout.Foldout(_lightFoldout, "Lighting Settings", true);
                    if (_lightFoldout) DrawLightSettings();

                    _exportFoldout = EditorGUILayout.Foldout(_exportFoldout, "Export Settings", true);
                    if (_exportFoldout) DrawExportSettings();

                    EditorGUILayout.Space(10);
                    DrawActionButtons();
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (_previewTexture != null)
            {
                float previewSize = _rightPanelWidth - 20;
                var rect = GUILayoutUtility.GetRect(previewSize, previewSize);
                EditorGUI.DrawPreviewTexture(rect, _previewTexture, null, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a prefab to preview.", MessageType.Info);
            }
        }

        private void DrawCameraSettings()
        {
            EditorGUI.indentLevel++;

            float newAz = EditorGUILayout.Slider("Azimuth", _settings.Azimuth, 0f, 360f);
            float newEl = EditorGUILayout.Slider("Elevation", _settings.Elevation, -90f, 90f);
            float newZoom = EditorGUILayout.Slider("Zoom", _settings.Zoom, 0.5f, 3f);
            float newOffX = EditorGUILayout.Slider("Offset X", _settings.CameraOffsetX, -2f, 2f);
            float newOffY = EditorGUILayout.Slider("Offset Y", _settings.CameraOffsetY, -2f, 2f);
            Color newBg = EditorGUILayout.ColorField("Background", _settings.BackgroundColor);
            var newBgTex = (Texture2D)EditorGUILayout.ObjectField(
                "Background Image", _settings.BackgroundTexture, typeof(Texture2D), false);

            if (!Mathf.Approximately(newAz, _settings.Azimuth) ||
                !Mathf.Approximately(newEl, _settings.Elevation) ||
                !Mathf.Approximately(newZoom, _settings.Zoom) ||
                !Mathf.Approximately(newOffX, _settings.CameraOffsetX) ||
                !Mathf.Approximately(newOffY, _settings.CameraOffsetY) ||
                newBg != _settings.BackgroundColor ||
                newBgTex != _settings.BackgroundTexture)
            {
                _settings.Azimuth = newAz;
                _settings.Elevation = newEl;
                _settings.Zoom = newZoom;
                _settings.CameraOffsetX = newOffX;
                _settings.CameraOffsetY = newOffY;
                _settings.BackgroundColor = newBg;
                _settings.BackgroundTexture = newBgTex;
                MarkPreviewDirty();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawLightSettings()
        {
            EditorGUI.indentLevel++;

            float newPitch = EditorGUILayout.Slider("Pitch", _settings.LightPitch, -90f, 90f);
            float newYaw = EditorGUILayout.Slider("Yaw", _settings.LightYaw, 0f, 360f);
            float newIntensity = EditorGUILayout.Slider("Intensity", _settings.LightIntensity, 0f, 3f);
            Color newColor = EditorGUILayout.ColorField("Color", _settings.LightColor);

            if (!Mathf.Approximately(newPitch, _settings.LightPitch) ||
                !Mathf.Approximately(newYaw, _settings.LightYaw) ||
                !Mathf.Approximately(newIntensity, _settings.LightIntensity) ||
                newColor != _settings.LightColor)
            {
                _settings.LightPitch = newPitch;
                _settings.LightYaw = newYaw;
                _settings.LightIntensity = newIntensity;
                _settings.LightColor = newColor;
                MarkPreviewDirty();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawExportSettings()
        {
            EditorGUI.indentLevel++;

            // Resolution popup
            int resIndex = Array.IndexOf(Resolutions, _settings.Resolution);
            if (resIndex < 0) resIndex = 3; // default to 1024
            int newResIndex = EditorGUILayout.Popup("Resolution", resIndex, ResolutionLabels);
            if (newResIndex != resIndex)
                _settings.Resolution = Resolutions[newResIndex];

            // Supersample popup
            int ssIndex = Array.IndexOf(SupersampleFactors, _settings.SupersampleFactor);
            if (ssIndex < 0) ssIndex = 2; // default to 4x
            int newSsIndex = EditorGUILayout.Popup("Supersample", ssIndex, SupersampleLabels);
            if (newSsIndex != ssIndex)
                _settings.SupersampleFactor = SupersampleFactors[newSsIndex];

            // Output folder
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Output Folder", GUILayout.Width(90));
                EditorGUILayout.LabelField(_outputFolder, EditorStyles.miniLabel);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string folder = EditorUtility.OpenFolderPanel("Select Output Folder", _outputFolder, "");
                    if (!string.IsNullOrEmpty(folder))
                    {
                        string dataPath = Application.dataPath;
                        _outputFolder = folder.StartsWith(dataPath)
                            ? "Assets" + folder.Substring(dataPath.Length)
                            : folder;
                        SaveSettings();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private void DrawActionButtons()
        {
            int selectedCount = _selectedGuids.Count;

            GUI.enabled = selectedCount > 0;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button($"Generate Selected ({selectedCount})", GUILayout.Height(30)))
                GenerateIcons(GetSelectedPrefabs());
            GUI.backgroundColor = Color.white;

            GUI.enabled = _prefabs.Count > 0;
            GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
            if (GUILayout.Button($"Generate All ({_prefabs.Count})", GUILayout.Height(30)))
                GenerateIcons(GetAllPrefabs());
            GUI.backgroundColor = Color.white;

            GUI.enabled = true;
        }

        // ─── Preview Rendering ────────────────────────────────────

        private void MarkPreviewDirty()
        {
            _previewDirty = true;
            _previewDirtyTime = EditorApplication.timeSinceStartup;
        }

        private void RenderPreview()
        {
            if (_previewPrefabIndex < 0 || _previewPrefabIndex >= _prefabs.Count)
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabs[_previewPrefabIndex].Path);
            if (prefab == null) return;

            CleanupPreview();

            // Use lower resolution for fast preview
            var previewSettings = _settings;
            previewSettings.Resolution = 256;
            previewSettings.SupersampleFactor = 1;

            _previewTexture = PrefabIconRenderer.RenderIcon(prefab, previewSettings);
        }

        private void CleanupPreview()
        {
            if (_previewTexture != null)
            {
                DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }
        }

        // ─── Export ───────────────────────────────────────────────

        private List<(string path, string name)> GetSelectedPrefabs()
        {
            var result = new List<(string, string)>();
            foreach (var p in _prefabs)
            {
                if (_selectedGuids.Contains(p.Guid))
                    result.Add((p.Path, p.Name));
            }
            return result;
        }

        private List<(string path, string name)> GetAllPrefabs()
        {
            var result = new List<(string, string)>();
            foreach (var p in _prefabs)
                result.Add((p.Path, p.Name));
            return result;
        }

        private void GenerateIcons(List<(string path, string name)> prefabs)
        {
            var generatedPaths = new List<string>();

            try
            {
                for (int i = 0; i < prefabs.Count; i++)
                {
                    var (path, name) = prefabs[i];

                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Generating Icons",
                        $"Rendering {name}... ({i + 1}/{prefabs.Count})",
                        (float)i / prefabs.Count))
                    {
                        break; // User cancelled
                    }

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    Texture2D icon = PrefabIconRenderer.RenderIcon(prefab, _settings);
                    if (icon == null) continue;

                    string assetPath = $"{_outputFolder}/{name}.png";
                    string fullPath = Path.Combine(Application.dataPath, "..", assetPath);

                    PrefabIconRenderer.SaveIconToPNG(icon, fullPath);
                    DestroyImmediate(icon);

                    generatedPaths.Add(assetPath);
                    Debug.Log($"[PrefabIconGenerator] Generated: {assetPath}");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (generatedPaths.Count == 0) return;

            // Import generated PNGs and configure as sprites
            AssetDatabase.Refresh();

            foreach (string assetPath in generatedPaths)
                PrefabIconRenderer.ConfigureSpriteImporter(assetPath);

            // Update thumbnail cache for the exported icons
            _thumbnailCache.Clear(); // Force reload from disk
            foreach (var (path, name) in prefabs)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                string pngPath = $"{_outputFolder}/{name}.png";
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
                if (tex != null)
                    _thumbnailCache[guid] = tex;
            }

            SaveSettings();

            Debug.Log($"[PrefabIconGenerator] Done! Generated {generatedPaths.Count} icon(s).");
            EditorUtility.DisplayDialog("Prefab Icon Generator",
                $"Generated {generatedPaths.Count} icon(s) in {_outputFolder}.",
                "OK");
        }

        // ─── Thumbnails ──────────────────────────────────────────

        private Texture2D GetThumbnail(string guid, string prefabPath)
        {
            // Check for cached exported PNG
            if (_thumbnailCache.TryGetValue(guid, out var cached))
                return cached;

            // Try to load exported PNG from output folder
            string name = Path.GetFileNameWithoutExtension(prefabPath);
            string pngPath = $"{_outputFolder}/{name}.png";
            var diskTex = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            if (diskTex != null)
            {
                _thumbnailCache[guid] = diskTex;
                return diskTex;
            }

            // Fall back to Unity's built-in asset preview (nice 3D render)
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return null;

            var preview = AssetPreview.GetAssetPreview(prefab);
            if (preview != null)
                return preview; // Don't cache — Unity manages this texture's lifecycle

            // Preview still loading — show mini thumbnail and keep repainting
            if (AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()))
                Repaint();

            return AssetPreview.GetMiniThumbnail(prefab);
        }

        // ─── Prefab List ─────────────────────────────────────────

        private void RefreshPrefabList()
        {
            _prefabs.Clear();
            _selectedGuids.Clear();
            _thumbnailCache.Clear();
            _previewPrefabIndex = -1;
            _lastClickedIndex = -1;

            if (!AssetDatabase.IsValidFolder(_sourceFolder))
                return;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { _sourceFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                _prefabs.Add(new PrefabEntry
                {
                    Guid = guid,
                    Path = path,
                    Name = Path.GetFileNameWithoutExtension(path)
                });
            }
        }

        // ─── Settings Persistence (EditorPrefs) ──────────────────

        private void LoadSettings()
        {
            _settings = IconRenderSettings.Default;

            _sourceFolder = EditorPrefs.GetString(PrefsPrefix + "SourceFolder", "Assets");
            _outputFolder = EditorPrefs.GetString(PrefsPrefix + "OutputFolder", "Assets");
            _leftPanelWidth = EditorPrefs.GetFloat(PrefsPrefix + "LeftWidth", 200f);
            _rightPanelWidth = EditorPrefs.GetFloat(PrefsPrefix + "RightWidth", 300f);
            _settings.Azimuth = EditorPrefs.GetFloat(PrefsPrefix + "Azimuth", _settings.Azimuth);
            _settings.Elevation = EditorPrefs.GetFloat(PrefsPrefix + "Elevation", _settings.Elevation);
            _settings.Zoom = EditorPrefs.GetFloat(PrefsPrefix + "Zoom", _settings.Zoom);
            _settings.LightPitch = EditorPrefs.GetFloat(PrefsPrefix + "LightPitch", _settings.LightPitch);
            _settings.LightYaw = EditorPrefs.GetFloat(PrefsPrefix + "LightYaw", _settings.LightYaw);
            _settings.LightIntensity = EditorPrefs.GetFloat(PrefsPrefix + "LightIntensity", _settings.LightIntensity);
            _settings.Resolution = EditorPrefs.GetInt(PrefsPrefix + "Resolution", _settings.Resolution);
            _settings.SupersampleFactor = EditorPrefs.GetInt(PrefsPrefix + "Supersample", _settings.SupersampleFactor);
            _thumbnailSize = EditorPrefs.GetInt(PrefsPrefix + "ThumbnailSize", _thumbnailSize);

            _settings.CameraOffsetX = EditorPrefs.GetFloat(PrefsPrefix + "OffsetX", 0f);
            _settings.CameraOffsetY = EditorPrefs.GetFloat(PrefsPrefix + "OffsetY", 0f);
            _settings.BackgroundColor = LoadColor(PrefsPrefix + "BgColor", _settings.BackgroundColor);
            _settings.LightColor = LoadColor(PrefsPrefix + "LightColor", _settings.LightColor);

            string bgTexPath = EditorPrefs.GetString(PrefsPrefix + "BgTexture", "");
            if (!string.IsNullOrEmpty(bgTexPath))
                _settings.BackgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(bgTexPath);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString(PrefsPrefix + "SourceFolder", _sourceFolder);
            EditorPrefs.SetString(PrefsPrefix + "OutputFolder", _outputFolder);
            EditorPrefs.SetFloat(PrefsPrefix + "LeftWidth", _leftPanelWidth);
            EditorPrefs.SetFloat(PrefsPrefix + "RightWidth", _rightPanelWidth);
            EditorPrefs.SetFloat(PrefsPrefix + "Azimuth", _settings.Azimuth);
            EditorPrefs.SetFloat(PrefsPrefix + "Elevation", _settings.Elevation);
            EditorPrefs.SetFloat(PrefsPrefix + "Zoom", _settings.Zoom);
            EditorPrefs.SetFloat(PrefsPrefix + "OffsetX", _settings.CameraOffsetX);
            EditorPrefs.SetFloat(PrefsPrefix + "OffsetY", _settings.CameraOffsetY);
            EditorPrefs.SetFloat(PrefsPrefix + "LightPitch", _settings.LightPitch);
            EditorPrefs.SetFloat(PrefsPrefix + "LightYaw", _settings.LightYaw);
            EditorPrefs.SetFloat(PrefsPrefix + "LightIntensity", _settings.LightIntensity);
            EditorPrefs.SetInt(PrefsPrefix + "Resolution", _settings.Resolution);
            EditorPrefs.SetInt(PrefsPrefix + "Supersample", _settings.SupersampleFactor);
            EditorPrefs.SetInt(PrefsPrefix + "ThumbnailSize", _thumbnailSize);

            SaveColor(PrefsPrefix + "BgColor", _settings.BackgroundColor);
            SaveColor(PrefsPrefix + "LightColor", _settings.LightColor);
            EditorPrefs.SetString(PrefsPrefix + "BgTexture",
                _settings.BackgroundTexture != null ? AssetDatabase.GetAssetPath(_settings.BackgroundTexture) : "");
        }

        private static Color LoadColor(string key, Color defaultValue)
        {
            string s = EditorPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(s)) return defaultValue;

            string[] parts = s.Split(',');
            if (parts.Length != 4) return defaultValue;

            if (float.TryParse(parts[0], out float r) &&
                float.TryParse(parts[1], out float g) &&
                float.TryParse(parts[2], out float b) &&
                float.TryParse(parts[3], out float a))
                return new Color(r, g, b, a);

            return defaultValue;
        }

        private static void SaveColor(string key, Color color)
        {
            EditorPrefs.SetString(key, $"{color.r},{color.g},{color.b},{color.a}");
        }
    }
}
