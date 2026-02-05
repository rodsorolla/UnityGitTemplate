using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

[CustomEditor(typeof(ShapeGenerator))]
public class ShapeGeneratorEditor : Editor
{
    // Serialized Properties - Core
    private SerializedProperty shapeType;
    private SerializedProperty prefabs;
    private SerializedProperty spacing;
    private SerializedProperty completeness;
    private SerializedProperty prefabFolders;

    // Quick Prefab Cache - dynamic per folder
    private static Dictionary<string, List<GameObject>> _folderPrefabCache = new Dictionary<string, List<GameObject>>();

    // Foldout states for each folder (by path)
    private Dictionary<string, bool> _folderFoldoutStates = new Dictionary<string, bool>();

    // Serialized Properties - Rotation
    private SerializedProperty randomRotation;
    private SerializedProperty alignmentMode;

    // Serialized Properties - Scale
    private SerializedProperty randomScale;
    private SerializedProperty scaleRange;
    private SerializedProperty scaleGradient;
    private SerializedProperty gradientStartScale;
    private SerializedProperty gradientEndScale;

    // Serialized Properties - Position
    private SerializedProperty positionJitter;
    private SerializedProperty jitterAmount;
    private SerializedProperty heightOffset;

    // Serialized Properties - Options
    private SerializedProperty autoPreview;

    // Foldout states
    private bool _showShapeConfig = true;
    private bool _showQuickPrefabs = true;
    private bool _showRotationSettings = false;
    private bool _showScaleSettings = false;
    private bool _showPositionSettings = false;
    private bool _showShapeSpecific = true;
    private bool _showActions = true;

    // Colors
    private static readonly Color HeaderColor = new Color(0.2f, 0.6f, 0.9f, 1f);
    private static readonly Color AccentColor = new Color(0.3f, 0.8f, 0.5f, 1f);
    private static readonly Color WarningColor = new Color(0.9f, 0.7f, 0.2f, 1f);

    // Quick Test state - use SessionState to persist across domain reloads
    private static string _previousScenePath
    {
        get => SessionState.GetString("ShapeGen_PreviousScenePath", "");
        set => SessionState.SetString("ShapeGen_PreviousScenePath", value);
    }
    private static bool _isQuickTestActive
    {
        get => SessionState.GetBool("ShapeGen_IsQuickTestActive", false);
        set => SessionState.SetBool("ShapeGen_IsQuickTestActive", value);
    }
    private static string _quickTestLevelPath
    {
        get => SessionState.GetString("ShapeGen_QuickTestLevelPath", "");
        set => SessionState.SetString("ShapeGen_QuickTestLevelPath", value);
    }

    // Constants for Quick Test
    private const string INIT_SCENE_PATH = "Assets/_Game/Scenes/Init.unity";
    private const string GAME_SCENE_PATH = "Assets/_Game/Scenes/Game.unity";
    private const string SNAKE_DATABASE_PATH = "Assets/_Game/Data/Snake_Database.asset";

    [InitializeOnLoadMethod]
    static void RegisterPlayModeCallback()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && _isQuickTestActive)
        {
            _isQuickTestActive = false;
            
            // Return to the previous scene
            if (!string.IsNullOrEmpty(_previousScenePath) && File.Exists(_previousScenePath))
            {
                EditorApplication.delayCall += () =>
                {
                    EditorSceneManager.OpenScene(_previousScenePath);
                    _previousScenePath = null;
                };
            }
        }
    }

    void OnEnable()
    {
        if (target == null || serializedObject == null)
            return;

        // Core
        shapeType = serializedObject.FindProperty("shapeType");
        prefabs = serializedObject.FindProperty("prefabs");
        spacing = serializedObject.FindProperty("spacing");
        completeness = serializedObject.FindProperty("completeness");
        prefabFolders = serializedObject.FindProperty("prefabFolders");

        // Load prefabs from configured folders
        RefreshPrefabCache();

        // Rotation
        randomRotation = serializedObject.FindProperty("randomRotation");
        alignmentMode = serializedObject.FindProperty("alignmentMode");

        // Scale
        randomScale = serializedObject.FindProperty("randomScale");
        scaleRange = serializedObject.FindProperty("scaleRange");
        scaleGradient = serializedObject.FindProperty("scaleGradient");
        gradientStartScale = serializedObject.FindProperty("gradientStartScale");
        gradientEndScale = serializedObject.FindProperty("gradientEndScale");

        // Position
        positionJitter = serializedObject.FindProperty("positionJitter");
        jitterAmount = serializedObject.FindProperty("jitterAmount");
        heightOffset = serializedObject.FindProperty("heightOffset");

        // Options
        autoPreview = serializedObject.FindProperty("autoPreview");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ShapeGenerator generator = (ShapeGenerator)target;

        DrawHeader(generator);
        EditorGUILayout.Space(5);
        
        DrawPreviewStatus(generator);
        EditorGUILayout.Space(10);

        // Shape Configuration Section
        _showShapeConfig = DrawFoldoutSection("Shape Configuration", _showShapeConfig, () =>
        {
            DrawShapeTypeSelector();
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(prefabs, new GUIContent("Prefabs"), true);
            if (prefabs.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Add prefabs to generate shapes", MessageType.Warning);
            }
            else if (prefabs.arraySize > 1)
            {
                DrawMiniLabel("Multiple prefabs = random variation");
            }
            
            // Quick Prefab Buttons
            EditorGUILayout.Space(5);
            DrawQuickPrefabButtons(generator);

            EditorGUILayout.Space(5);
            DrawSliderWithLabel(spacing, "Spacing", 0.1f, 10f, "Horizontal distance between objects");

            // Show vertical spacing for 3D shapes that use it
            ShapeGenerator.ShapeType currentShape = (ShapeGenerator.ShapeType)shapeType.enumValueIndex;
            if (currentShape == ShapeGenerator.ShapeType.Pyramid ||
                currentShape == ShapeGenerator.ShapeType.Cone ||
                currentShape == ShapeGenerator.ShapeType.Cylinder)
            {
                var verticalSpacing = serializedObject.FindProperty("verticalSpacing");
                DrawSliderWithLabel(verticalSpacing, "Vertical Spacing", 0.1f, 10f, "Vertical distance between layers");
            }

            DrawSliderWithLabel(completeness, "Completeness", 0f, 1f, "1 = Full shape, 0.5 = Half");
        });

        // Shape-Specific Settings
        _showShapeSpecific = DrawFoldoutSection(GetShapeIcon() + " " + shapeType.enumDisplayNames[shapeType.enumValueIndex] + " Settings", _showShapeSpecific, () =>
        {
            DrawShapeSettings(generator);
        });

        // Rotation Settings
        _showRotationSettings = DrawFoldoutSection("🔄 Rotation", _showRotationSettings, () =>
        {
            DrawToggleWithIndent(randomRotation, "Random Y Rotation", "Random 0-360° rotation");
            EditorGUILayout.Space(3);
            EditorGUILayout.PropertyField(alignmentMode, new GUIContent("Alignment Mode"));
            DrawMiniLabel("Face Center • Face Outward • Along Path");
        });

        // Scale Settings
        _showScaleSettings = DrawFoldoutSection("📐 Scale", _showScaleSettings, () =>
        {
            DrawToggleWithIndent(randomScale, "Random Scale", "Randomize object sizes");
            if (randomScale.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(scaleRange, new GUIContent("Scale Range"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            DrawToggleWithIndent(scaleGradient, "Scale Gradient", "Progressive size change");
            if (scaleGradient.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawSliderWithLabel(gradientStartScale, "Start Scale", 0.1f, 3f);
                DrawSliderWithLabel(gradientEndScale, "End Scale", 0.1f, 3f);
                EditorGUI.indentLevel--;
            }
        });

        // Position Settings
        _showPositionSettings = DrawFoldoutSection("📍 Position", _showPositionSettings, () =>
        {
            DrawToggleWithIndent(positionJitter, "Position Jitter", "Random offset for organic look");
            if (positionJitter.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawSliderWithLabel(jitterAmount, "Jitter Amount", 0f, 2f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            DrawSliderWithLabel(heightOffset, "Height Offset", -10f, 10f, "Elevate on Y axis");
        });

        EditorGUILayout.Space(10);

        // Actions Section
        _showActions = DrawFoldoutSection("⚡ Actions", _showActions, () =>
        {
            DrawActionsSection(generator);
        });

        serializedObject.ApplyModifiedProperties();
    }

    // ===== HEADER =====
    
    private void DrawHeader(ShapeGenerator generator)
    {
        EditorGUILayout.Space(3);
        
        // Header
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = HeaderColor }
        };
        
        EditorGUILayout.LabelField("Shape Generator", headerStyle);
        
        var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            fontStyle = FontStyle.Italic
        };
        EditorGUILayout.LabelField("Create procedural object arrangements", subtitleStyle);
    }

    private void DrawPreviewStatus(ShapeGenerator generator)
    {
        EditorGUILayout.BeginHorizontal();
        
        // Auto Preview Toggle
        var toggleStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontStyle = autoPreview.boolValue ? FontStyle.Bold : FontStyle.Normal
        };
        GUI.backgroundColor = autoPreview.boolValue ? AccentColor : new Color(0.4f, 0.4f, 0.4f, 1f);
        if (GUILayout.Button(autoPreview.boolValue ? "● Live Preview ON" : "○ Live Preview OFF", toggleStyle, GUILayout.Height(20)))
        {
            autoPreview.boolValue = !autoPreview.boolValue;
            if (autoPreview.boolValue)
            {
                generator.GeneratePreview();
            }
        }
        GUI.backgroundColor = Color.white;


        EditorGUILayout.EndHorizontal();

        // Status bar
        if (generator.previewRoot != null)
        {
            int objectCount = generator.previewRoot.transform.childCount;
            DrawStatusBar($"Preview: {objectCount} objects", AccentColor);
        }
        else
        {
            DrawStatusBar("No preview active", Color.gray);
        }
    }

    private void DrawStatusBar(string message, Color color)
    {
        var rect = EditorGUILayout.GetControlRect(false, 18);
        var bgColor = new Color(color.r, color.g, color.b, 0.2f);
        EditorGUI.DrawRect(rect, bgColor);
        
        var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            normal = { textColor = color },
            fontStyle = FontStyle.Bold
        };
        EditorGUI.LabelField(rect, message, style);
    }

    // ===== SHAPE TYPE SELECTOR =====

    private void DrawShapeTypeSelector()
    {
        EditorGUILayout.LabelField("Shape Type", EditorStyles.miniBoldLabel);
        
        // 2D Shapes - First Row
        EditorGUILayout.BeginHorizontal();
        DrawShapeButton("Line", "━");
        DrawShapeButton("Grid", "⊞");
        DrawShapeButton("Square", "□");
        DrawShapeButton("Circle", "○");
        DrawShapeButton("Ring", "◎");
        DrawShapeButton("Spiral", "◌");
        DrawShapeButton("Star", "☆");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(2);
        
        // 3D Shapes - Second Row  
        EditorGUILayout.BeginHorizontal();
        DrawShapeButton("Pyramid", "△");
        DrawShapeButton("Cone", "▲");
        DrawShapeButton("Cylinder", "▮");
        DrawShapeButton("Sphere", "●");
        DrawShapeButton("Helix", "⌀");
        GUILayout.FlexibleSpace(); // Fill remaining space
        EditorGUILayout.EndHorizontal();
    }

    private void DrawShapeButton(string shapeName, string icon)
    {
        int shapeIndex = System.Array.IndexOf(shapeType.enumDisplayNames, shapeName);
        if (shapeIndex < 0) return;
        
        bool isSelected = shapeType.enumValueIndex == shapeIndex;
        
        GUI.backgroundColor = isSelected ? HeaderColor : new Color(0.35f, 0.35f, 0.35f, 1f);
        
        var style = new GUIStyle(GUI.skin.button)
        {
            fontSize = 9,
            fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
            padding = new RectOffset(2, 2, 2, 2),
            alignment = TextAnchor.MiddleCenter
        };
        
        string buttonText = icon + "\n" + shapeName;
        
        if (GUILayout.Button(buttonText, style, GUILayout.Width(50), GUILayout.Height(32)))
        {
            shapeType.enumValueIndex = shapeIndex;
            if (autoPreview.boolValue)
            {
                serializedObject.ApplyModifiedProperties();
                ((ShapeGenerator)target).GeneratePreview();
            }
        }
        
        GUI.backgroundColor = Color.white;
    }

    private string GetShapeIcon()
    {
        string shapeName = shapeType.enumDisplayNames[shapeType.enumValueIndex];
        return shapeName switch
        {
            "Line" => "━",
            "Grid" => "⊞",
            "Square" => "□",
            "Circle" => "○",
            "Ring" => "◎",
            "Spiral" => "◌",
            "Star" => "☆",
            "Pyramid" => "△",
            "Cone" => "▲",
            "Cylinder" => "▮",
            "Sphere" => "●",
            "Helix" => "⌀",
            _ => "•"
        };
    }

    // ===== FOLDOUT SECTION =====

    private bool DrawFoldoutSection(string title, bool isExpanded, System.Action drawContent)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Header
        var headerRect = EditorGUILayout.GetControlRect(false, 22);
        var bgColor = isExpanded ? new Color(0.25f, 0.25f, 0.25f, 0.3f) : new Color(0.2f, 0.2f, 0.2f, 0.2f);
        EditorGUI.DrawRect(headerRect, bgColor);
        
        var foldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };
        
        isExpanded = EditorGUI.Foldout(headerRect, isExpanded, " " + title, true, foldoutStyle);
        
        if (isExpanded)
        {
            EditorGUILayout.Space(5);
            drawContent();
            EditorGUILayout.Space(5);
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
        
        return isExpanded;
    }

    // ===== HELPER DRAWING METHODS =====

    private void DrawSliderWithLabel(SerializedProperty prop, string label, float min, float max, string tooltip = null)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(EditorGUIUtility.labelWidth - 15));
        prop.floatValue = EditorGUILayout.Slider(prop.floatValue, min, max);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToggleWithIndent(SerializedProperty prop, string label, string tooltip = null)
    {
        EditorGUILayout.BeginHorizontal();
        prop.boolValue = EditorGUILayout.Toggle(prop.boolValue, GUILayout.Width(15));
        EditorGUILayout.LabelField(new GUIContent(label, tooltip));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawMiniLabel(string text)
    {
        var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            alignment = TextAnchor.MiddleLeft
        };
        EditorGUILayout.LabelField(text, style);
    }

    // ===== QUICK PREFAB BUTTONS =====

    private void RefreshPrefabCache()
    {
        _folderPrefabCache.Clear();

        ShapeGenerator generator = (ShapeGenerator)target;
        if (generator.prefabFolders == null) return;

        foreach (string folderPath in generator.prefabFolders)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                continue;

            var prefabList = new List<GameObject>();
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Skip pre-made shapes
                if (path.Contains("Shape") || path.Contains("shape")) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    prefabList.Add(prefab);
            }

            if (prefabList.Count > 0)
            {
                _folderPrefabCache[folderPath] = prefabList;
            }
        }
    }

    private string GetFolderDisplayName(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return "Unknown";
        return Path.GetFileName(folderPath.TrimEnd('/', '\\'));
    }

    private void DrawQuickPrefabButtons(ShapeGenerator generator)
    {
        _showQuickPrefabs = EditorGUILayout.Foldout(_showQuickPrefabs, "Quick Prefab Selection", true, EditorStyles.foldoutHeader);

        if (!_showQuickPrefabs) return;

        EditorGUI.indentLevel++;

        // Folder management buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Folder", GUILayout.Height(20)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Prefab Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Convert to relative path
                string relativePath = selectedPath;
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }

                if (!generator.prefabFolders.Contains(relativePath))
                {
                    Undo.RecordObject(generator, "Add Prefab Folder");
                    generator.prefabFolders.Add(relativePath);
                    EditorUtility.SetDirty(generator);
                    RefreshPrefabCache();
                }
            }
        }
        if (GUILayout.Button("↻ Refresh", GUILayout.Width(70), GUILayout.Height(20)))
        {
            RefreshPrefabCache();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Draw each folder as a category
        if (generator.prefabFolders.Count == 0)
        {
            EditorGUILayout.HelpBox("No folders configured. Click '+ Add Folder' to add prefab folders.", MessageType.Info);
        }
        else
        {
            // Track folders to remove (can't modify during iteration)
            string folderToRemove = null;

            foreach (string folderPath in generator.prefabFolders)
            {
                if (string.IsNullOrEmpty(folderPath)) continue;

                string folderName = GetFolderDisplayName(folderPath);

                // Initialize foldout state if needed
                if (!_folderFoldoutStates.ContainsKey(folderPath))
                    _folderFoldoutStates[folderPath] = true;

                EditorGUILayout.BeginHorizontal();

                // Check if folder has prefabs cached
                bool hasPrefabs = _folderPrefabCache.ContainsKey(folderPath);
                int prefabCount = hasPrefabs ? _folderPrefabCache[folderPath].Count : 0;

                string label = hasPrefabs ? $"{folderName} ({prefabCount})" : $"{folderName} (not found)";
                _folderFoldoutStates[folderPath] = EditorGUILayout.Foldout(_folderFoldoutStates[folderPath], label, true);

                // Remove button
                GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f, 1f);
                if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(16)))
                {
                    folderToRemove = folderPath;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                if (_folderFoldoutStates[folderPath] && hasPrefabs)
                {
                    DrawPrefabButtonGrid(_folderPrefabCache[folderPath], generator);
                    EditorGUILayout.Space(5);
                }
            }

            // Remove folder if requested
            if (folderToRemove != null)
            {
                Undo.RecordObject(generator, "Remove Prefab Folder");
                generator.prefabFolders.Remove(folderToRemove);
                _folderFoldoutStates.Remove(folderToRemove);
                _folderPrefabCache.Remove(folderToRemove);
                EditorUtility.SetDirty(generator);
            }
        }

        EditorGUI.indentLevel--;
    }

    private void DrawPrefabButtonGrid(List<GameObject> prefabList, ShapeGenerator generator)
    {
        const int buttonsPerRow = 6;
        const float buttonSize = 45f;

        int count = 0;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(15); // Indent

        foreach (var prefab in prefabList)
        {
            if (prefab == null) continue;

            // Get preview texture
            Texture2D preview = AssetPreview.GetAssetPreview(prefab);
            if (preview == null)
                preview = AssetPreview.GetMiniThumbnail(prefab);

            // Check if this prefab is currently selected (in the prefabs array)
            bool isSelected = false;
            for (int i = 0; i < prefabs.arraySize; i++)
            {
                if (prefabs.GetArrayElementAtIndex(i).objectReferenceValue == prefab)
                {
                    isSelected = true;
                    break;
                }
            }

            // Button style
            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(2, 2, 2, 2),
                margin = new RectOffset(1, 1, 1, 1)
            };

            // Highlight if selected
            if (isSelected)
                GUI.backgroundColor = AccentColor;

            // Tooltip includes shift hint
            string tooltip = prefab.name + "\n(Shift+Click to add)";
            GUIContent content = preview != null 
                ? new GUIContent(preview, tooltip)
                : new GUIContent(prefab.name.Substring(0, Mathf.Min(3, prefab.name.Length)), tooltip);

            if (GUILayout.Button(content, btnStyle, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
            {
                // Check if Shift is held to add to existing prefabs
                if (Event.current.shift)
                {
                    AddPrefab(generator, prefab);
                }
                else
                {
                    SetPrefab(generator, prefab);
                }
            }

            GUI.backgroundColor = Color.white;
            count++;

            if (count % buttonsPerRow == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void SetPrefab(ShapeGenerator generator, GameObject prefab)
    {
        Undo.RecordObject(generator, "Set Shape Prefab");
        
        // Clear existing and set new prefab
        prefabs.ClearArray();
        prefabs.InsertArrayElementAtIndex(0);
        prefabs.GetArrayElementAtIndex(0).objectReferenceValue = prefab;
        
        serializedObject.ApplyModifiedProperties();

        // Regenerate preview if auto-preview is on
        if (generator.autoPreview)
        {
            generator.GeneratePreview();
        }

        EditorUtility.SetDirty(generator);
    }

    private void AddPrefab(ShapeGenerator generator, GameObject prefab)
    {
        // Check if prefab is already in the list
        for (int i = 0; i < prefabs.arraySize; i++)
        {
            if (prefabs.GetArrayElementAtIndex(i).objectReferenceValue == prefab)
            {
                // Already exists, don't add duplicate
                return;
            }
        }
        
        Undo.RecordObject(generator, "Add Shape Prefab");
        
        // Add to existing prefabs
        int newIndex = prefabs.arraySize;
        prefabs.InsertArrayElementAtIndex(newIndex);
        prefabs.GetArrayElementAtIndex(newIndex).objectReferenceValue = prefab;
        
        serializedObject.ApplyModifiedProperties();

        // Regenerate preview if auto-preview is on
        if (generator.autoPreview)
        {
            generator.GeneratePreview();
        }

        EditorUtility.SetDirty(generator);
    }

    // ===== ACTIONS SECTION =====

    private void DrawActionsSection(ShapeGenerator generator)
    {
        // Manual regenerate (only if auto preview is off)
        if (!generator.autoPreview)
        {
            GUI.backgroundColor = WarningColor;
            if (GUILayout.Button("Regenerate Preview", GUILayout.Height(28)))
            {
                Undo.RecordObject(generator, "Regenerate Shape Preview");
                generator.GeneratePreview();
                EditorUtility.SetDirty(generator);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = generator.previewRoot != null;
        
        // Accept Shape button
        GUI.backgroundColor = AccentColor;
        var acceptStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold
        };
        if (GUILayout.Button("Accept Shape", acceptStyle, GUILayout.Height(30)))
        {
            Undo.RecordObject(generator, "Accept Shape");
            generator.AcceptShape();
            EditorUtility.SetDirty(generator);
        }
        GUI.backgroundColor = Color.white;

        // Save as Prefab button
        GUI.backgroundColor = HeaderColor;
        if (GUILayout.Button("Save Prefab", acceptStyle, GUILayout.Height(30)))
        {
            generator.SaveAsPrefab();
        }
        GUI.backgroundColor = Color.white;
        
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        // Tips
        EditorGUILayout.Space(3);
        DrawMiniLabel(generator.previewRoot != null 
            ? "Accept to keep objects, or Save as reusable prefab" 
            : "Add prefabs above to start generating");
        
        // Quick Test Section
        EditorGUILayout.Space(10);
    }

    // ===== SHAPE-SPECIFIC SETTINGS =====

    void DrawShapeSettings(ShapeGenerator generator)
    {
        ShapeGenerator.ShapeType shape = (ShapeGenerator.ShapeType)shapeType.enumValueIndex;

        switch (shape)
        {
            case ShapeGenerator.ShapeType.Line:
                DrawLineSettings();
                break;
            case ShapeGenerator.ShapeType.Grid:
                DrawGridSettings();
                break;
            case ShapeGenerator.ShapeType.Square:
                DrawSquareSettings();
                break;
            case ShapeGenerator.ShapeType.Circle:
                DrawCircleSettings();
                break;
            case ShapeGenerator.ShapeType.Ring:
                DrawRingSettings();
                break;
            case ShapeGenerator.ShapeType.Spiral:
                DrawSpiralSettings();
                break;
            case ShapeGenerator.ShapeType.Pyramid:
                DrawPyramidSettings();
                break;
            case ShapeGenerator.ShapeType.Cone:
                DrawConeSettings();
                break;
            case ShapeGenerator.ShapeType.Cylinder:
                DrawCylinderSettings();
                break;
            case ShapeGenerator.ShapeType.Sphere:
                DrawSphereSettings();
                break;
            case ShapeGenerator.ShapeType.Helix:
                DrawHelixSettings();
                break;
            case ShapeGenerator.ShapeType.Star:
                DrawStarSettings();
                break;
        }
    }

    void DrawLineSettings()
    {
        DrawSliderWithLabel(serializedObject.FindProperty("lineSize"), "Length", 1f, 100f);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lineDirection"), new GUIContent("Direction"));

        EditorGUILayout.Space(5);
        SerializedProperty useWave = serializedObject.FindProperty("useWave");
        DrawToggleWithIndent(useWave, "Wave Pattern", "Creates sine wave along line");

        if (useWave.boolValue)
        {
            EditorGUI.indentLevel++;
            DrawSliderWithLabel(serializedObject.FindProperty("waveFrequency"), "Frequency", 0.1f, 10f);
            DrawSliderWithLabel(serializedObject.FindProperty("waveAmplitude"), "Amplitude", 0.1f, 10f);
            EditorGUI.indentLevel--;
        }
    }

    void DrawGridSettings()
    {
        DrawSliderWithLabel(serializedObject.FindProperty("gridSizeX"), "Width", 1f, 50f);
        DrawSliderWithLabel(serializedObject.FindProperty("gridSizeZ"), "Depth", 1f, 50f);
        DrawToggleWithIndent(serializedObject.FindProperty("fillGrid"), "Fill Grid", "Uncheck for border only");
    }

    void DrawSquareSettings()
    {
        DrawSliderWithLabel(serializedObject.FindProperty("squareSizeUnits"), "Size", 1f, 50f);
        DrawToggleWithIndent(serializedObject.FindProperty("fillSquare"), "Fill Square", "Uncheck for border only");
    }

    void DrawCircleSettings()
    {
        DrawSliderWithLabel(serializedObject.FindProperty("radius"), "Radius", 0.5f, 20f);
        DrawToggleWithIndent(serializedObject.FindProperty("fillCircle"), "Fill Circle", "Uncheck for border only");
    }

    void DrawRingSettings()
    {
        DrawSliderWithLabel(serializedObject.FindProperty("radius"), "Outer Radius", 0.5f, 20f);
        DrawSliderWithLabel(serializedObject.FindProperty("innerRadius"), "Inner Radius", 0.1f, 19f);
        DrawMiniLabel("Creates a donut/ring shape");
    }

    void DrawSpiralSettings()
    {
        DrawSliderWithLabel(serializedObject.FindProperty("spiralRotations"), "Rotations", 0.5f, 10f);
        DrawSliderWithLabel(serializedObject.FindProperty("spiralRadiusGrowth"), "Radius Growth", 0.1f, 5f);
        DrawSliderWithLabel(serializedObject.FindProperty("spiralDecay"), "Decay", 0.2f, 5f);
        DrawMiniLabel("Decay <1: spread center | >1: spread edge");
    }

    void DrawPyramidSettings()
    {
        DrawSliderWithLabel(serializedObject.FindProperty("pyramidSize"), "Base Size", 1f, 30f);
    }

    void DrawConeSettings()
    {
        DrawSliderWithLabel(serializedObject.FindProperty("coneHeightUnits"), "Height", 1f, 30f);
        DrawSliderWithLabel(serializedObject.FindProperty("coneRadius"), "Base Radius", 0.5f, 20f);
        DrawToggleWithIndent(serializedObject.FindProperty("fillCone"), "Fill Layers");
    }

    void DrawCylinderSettings()
    {
        DrawSliderWithLabel(serializedObject.FindProperty("cylinderHeightUnits"), "Height", 1f, 30f);
        DrawSliderWithLabel(serializedObject.FindProperty("cylinderRadius"), "Radius", 0.5f, 20f);
        DrawToggleWithIndent(serializedObject.FindProperty("fillCylinder"), "Fill Layers");
    }

    void DrawSphereSettings()
    {
        var sphereRings = serializedObject.FindProperty("sphereRings");
        EditorGUILayout.IntSlider(sphereRings, 3, 20, new GUIContent("Rings"));
        DrawSliderWithLabel(serializedObject.FindProperty("sphereRadius"), "Radius", 0.5f, 20f);
        DrawToggleWithIndent(serializedObject.FindProperty("fillSphere"), "Fill Sphere");
    }

    void DrawHelixSettings()
    {
        DrawSliderWithLabel(serializedObject.FindProperty("helixHeight"), "Height", 1f, 50f);
        DrawSliderWithLabel(serializedObject.FindProperty("helixRotations"), "Rotations", 0.5f, 10f);
        var pointsPerRot = serializedObject.FindProperty("helixPointsPerRotation");
        EditorGUILayout.IntSlider(pointsPerRot, 5, 50, new GUIContent("Points/Rotation"));
        DrawSliderWithLabel(serializedObject.FindProperty("helixRadius"), "Radius", 0.5f, 20f);
    }

    void DrawStarSettings()
    {
        var starPoints = serializedObject.FindProperty("starPoints");
        EditorGUILayout.IntSlider(starPoints, 3, 12, new GUIContent("Points"));
        DrawSliderWithLabel(serializedObject.FindProperty("starOuterRadius"), "Outer Radius", 0.5f, 20f);
        DrawSliderWithLabel(serializedObject.FindProperty("starInnerRadius"), "Inner Radius", 0.1f, 19f);
    }
}