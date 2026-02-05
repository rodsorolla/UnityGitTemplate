using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace Sorolla.Editor
{
    /// <summary>
    /// Editor tool for visualizing and editing UV-based gradient coloring on FBX models.
    /// Groups mesh vertices by UV proximity to identify color regions (e.g., couch body vs feet).
    /// Does not modify the original mesh - creates a new mesh asset on save.
    /// </summary>
    public class GradientColorEditor : EditorWindow
    {
        // Common texture property names across different shaders
        private static readonly string[] TexturePropertyNames = new string[]
        {
            "_BaseMap",      // URP Lit
            "_MainTex",      // Standard, Legacy
            "_Albedo",       // Some custom shaders
            "_Texture",      // Generic
        };
        private const float UVClusterThreshold = 0.15f; // UVs within this distance are grouped together

        private MeshRenderer _targetRenderer;
        private MeshFilter _targetMeshFilter;
        private Mesh _originalMesh;
        private Mesh _previewMesh;
        private Material _material;
        private Texture2D _gradientTexture;

        private List<UVRegion> _uvRegions = new List<UVRegion>();
        private int _selectedRegionIndex = -1;
        private string _meshName = "NewMesh";
        private Vector2 _scrollPosition;
        private bool _isPreviewing;

        // Preview rendering
        private PreviewRenderUtility _previewRenderUtility;
        private Vector2 _previewRotation = new Vector2(25f, -135f);
        private float _previewZoom = 1f;

        // Gradient and preview display
        private const float GradientSize = 280f;
        private const float MarkerSize = 14f;
        private const float PreviewSize = 280f;

        // Configurable output paths (stored in EditorPrefs per project)
        private const string PrefabFolderPrefKey = "GradientColorEditor_PrefabFolder";
        private const string MeshFolderPrefKey = "GradientColorEditor_MeshFolder";
        private const string DefaultPrefabFolder = "Assets/Prefabs/ColorVariants";
        private const string DefaultMeshFolder = "Assets/Meshes";

        private static string PrefabFolder
        {
            get => EditorPrefs.GetString(PrefabFolderPrefKey, DefaultPrefabFolder);
            set => EditorPrefs.SetString(PrefabFolderPrefKey, value);
        }

        private static string MeshFolder
        {
            get => EditorPrefs.GetString(MeshFolderPrefKey, DefaultMeshFolder);
            set => EditorPrefs.SetString(MeshFolderPrefKey, value);
        }

        // Region colors for visualization
        private static readonly Color[] RegionColors = new Color[]
        {
            new Color(1f, 0.3f, 0.3f, 1f),    // Red
            new Color(0.3f, 1f, 0.3f, 1f),    // Green
            new Color(0.3f, 0.5f, 1f, 1f),    // Blue
            new Color(1f, 1f, 0.3f, 1f),      // Yellow
            new Color(1f, 0.3f, 1f, 1f),      // Magenta
            new Color(0.3f, 1f, 1f, 1f),      // Cyan
        };

        /// <summary>
        /// A UV region groups multiple mesh vertex islands that share similar UV positions.
        /// This represents a "color zone" on the model (e.g., couch body, feet).
        /// </summary>
        private class UVRegion
        {
            public string Name;
            public List<int> VertexIndices = new List<int>();
            public Vector2 CenterUV;
            public Vector2 OriginalCenterUV;
            public Color DisplayColor;
        }

        [MenuItem("Tools/Sorolla/Gradient Color Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<GradientColorEditor>("Gradient Color Editor");
            window.minSize = new Vector2(500, 700);
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            RevertPreview();
            CleanupPreviewMesh();
            CleanupPreviewRenderUtility();
        }

        private void CleanupPreviewRenderUtility()
        {
            if (_previewRenderUtility != null)
            {
                _previewRenderUtility.Cleanup();
                _previewRenderUtility = null;
            }
        }

        private void InitPreviewRenderUtility()
        {
            if (_previewRenderUtility == null)
            {
                _previewRenderUtility = new PreviewRenderUtility();
                _previewRenderUtility.cameraFieldOfView = 30f;
            }
        }

        private void OnSelectionChanged()
        {
            RevertPreview();
            CleanupPreviewMesh();

            _targetRenderer = null;
            _targetMeshFilter = null;
            _originalMesh = null;
            _material = null;
            _gradientTexture = null;
            _uvRegions.Clear();
            _selectedRegionIndex = -1;

            if (Selection.activeGameObject != null)
            {
                _targetRenderer = Selection.activeGameObject.GetComponent<MeshRenderer>();
                _targetMeshFilter = Selection.activeGameObject.GetComponent<MeshFilter>();

                if (_targetRenderer != null && _targetMeshFilter != null)
                {
                    SetupForMesh();
                }
            }

            Repaint();
        }

        private void SetupForMesh()
        {
            if (_targetMeshFilter.sharedMesh == null) return;

            _originalMesh = _targetMeshFilter.sharedMesh;

            // Get material and texture
            if (_targetRenderer.sharedMaterials.Length > 0 && _targetRenderer.sharedMaterials[0] != null)
            {
                _material = _targetRenderer.sharedMaterials[0];

                // Try to find the gradient texture using common property names
                _gradientTexture = FindGradientTexture(_material);

                if (_gradientTexture == null)
                {
                    Debug.LogWarning($"[GradientColorEditor] Could not find gradient texture on material '{_material.name}'. " +
                        $"Shader: {_material.shader.name}");
                }
            }

            // Analyze and group UV regions
            AnalyzeUVRegions();

            // Set default mesh name from original
            _meshName = _originalMesh.name + "_Colored";
        }

        private void CleanupPreviewMesh()
        {
            if (_previewMesh != null)
            {
                DestroyImmediate(_previewMesh);
                _previewMesh = null;
            }
        }

        /// <summary>
        /// Finds the gradient/main texture on a material by trying common property names.
        /// </summary>
        private Texture2D FindGradientTexture(Material material)
        {
            if (material == null) return null;

            // First try common property names
            foreach (string propName in TexturePropertyNames)
            {
                if (material.HasProperty(propName))
                {
                    Texture tex = material.GetTexture(propName);
                    if (tex is Texture2D tex2D)
                    {
                        return tex2D;
                    }
                }
            }

            // Fallback: try material.mainTexture
            if (material.mainTexture is Texture2D mainTex)
            {
                return mainTex;
            }

            // Last resort: iterate through all texture properties
            Shader shader = material.shader;
            int propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++)
            {
                if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    string propName = shader.GetPropertyName(i);
                    Texture tex = material.GetTexture(propName);
                    if (tex is Texture2D tex2D)
                    {
                        Debug.Log($"[GradientColorEditor] Found texture in property '{propName}'");
                        return tex2D;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets UV coordinates from a mesh, working even on non-readable meshes using MeshDataArray.
        /// </summary>
        private Vector2[] GetMeshUVs(Mesh mesh)
        {
            if (mesh == null) return null;

            // Try direct access first (faster, works on readable meshes)
            if (mesh.isReadable)
            {
                return mesh.uv;
            }

            // For non-readable meshes, use MeshDataArray API
            try
            {
                using (var dataArray = Mesh.AcquireReadOnlyMeshData(mesh))
                {
                    var data = dataArray[0];
                    int vertexCount = data.vertexCount;

                    if (vertexCount == 0) return null;

                    // Check if mesh has UV data
                    if (!data.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))
                    {
                        Debug.LogWarning($"[GradientColorEditor] Mesh has no UV data");
                        return null;
                    }

                    var uvs = new NativeArray<Vector2>(vertexCount, Allocator.Temp);
                    data.GetUVs(0, uvs);

                    Vector2[] result = uvs.ToArray();
                    uvs.Dispose();
                    return result;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GradientColorEditor] Failed to read UVs: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets triangles from a mesh, working even on non-readable meshes.
        /// </summary>
        private int[] GetMeshTriangles(Mesh mesh)
        {
            if (mesh == null) return null;

            // Try direct access first
            if (mesh.isReadable)
            {
                return mesh.triangles;
            }

            // For non-readable meshes, use MeshDataArray API
            try
            {
                using (var dataArray = Mesh.AcquireReadOnlyMeshData(mesh))
                {
                    var data = dataArray[0];

                    int totalIndices = 0;
                    for (int i = 0; i < data.subMeshCount; i++)
                    {
                        totalIndices += (int)data.GetSubMesh(i).indexCount;
                    }

                    if (totalIndices == 0) return null;

                    var indices = new NativeArray<int>(totalIndices, Allocator.Temp);
                    int offset = 0;

                    for (int i = 0; i < data.subMeshCount; i++)
                    {
                        var subMesh = data.GetSubMesh(i);
                        var subIndices = new NativeArray<int>((int)subMesh.indexCount, Allocator.Temp);
                        data.GetIndices(subIndices, i);

                        NativeArray<int>.Copy(subIndices, 0, indices, offset, (int)subMesh.indexCount);
                        offset += (int)subMesh.indexCount;
                        subIndices.Dispose();
                    }

                    int[] result = indices.ToArray();
                    indices.Dispose();
                    return result;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GradientColorEditor] Failed to read triangles: {e.Message}");
                return null;
            }
        }

        private void AnalyzeUVRegions()
        {
            _uvRegions.Clear();

            if (_originalMesh == null) return;

            // Get UVs - try direct access first, then use MeshDataArray for non-readable meshes
            Vector2[] uvs = GetMeshUVs(_originalMesh);
            if (uvs == null || uvs.Length == 0)
            {
                Debug.LogWarning($"[GradientColorEditor] Could not read UVs from mesh '{_originalMesh.name}'");
                return;
            }

            int[] triangles = GetMeshTriangles(_originalMesh);
            if (triangles == null || triangles.Length == 0)
            {
                Debug.LogWarning($"[GradientColorEditor] Could not read triangles from mesh '{_originalMesh.name}'");
                return;
            }

            // Step 1: Find connected vertex islands using triangle adjacency
            List<HashSet<int>> islands = FindConnectedIslands(uvs.Length, triangles);

            // Step 2: Calculate center UV for each island
            List<(HashSet<int> vertices, Vector2 center)> islandData = new List<(HashSet<int>, Vector2)>();
            foreach (var island in islands)
            {
                Vector2 sum = Vector2.zero;
                foreach (int v in island)
                {
                    sum += uvs[v];
                }
                Vector2 center = sum / island.Count;
                islandData.Add((island, center));
            }

            // Step 3: Cluster islands by UV proximity
            bool[] merged = new bool[islandData.Count];
            int regionIndex = 0;

            for (int i = 0; i < islandData.Count; i++)
            {
                if (merged[i]) continue;

                UVRegion region = new UVRegion();
                region.DisplayColor = RegionColors[regionIndex % RegionColors.Length];

                Vector2 sumCenter = Vector2.zero;
                int totalVerts = 0;

                // Start with this island
                foreach (int v in islandData[i].vertices)
                {
                    region.VertexIndices.Add(v);
                }
                sumCenter += islandData[i].center * islandData[i].vertices.Count;
                totalVerts += islandData[i].vertices.Count;
                merged[i] = true;

                // Find all islands with similar UV centers and merge them
                for (int j = i + 1; j < islandData.Count; j++)
                {
                    if (merged[j]) continue;

                    float distance = Vector2.Distance(islandData[i].center, islandData[j].center);
                    if (distance < UVClusterThreshold)
                    {
                        foreach (int v in islandData[j].vertices)
                        {
                            region.VertexIndices.Add(v);
                        }
                        sumCenter += islandData[j].center * islandData[j].vertices.Count;
                        totalVerts += islandData[j].vertices.Count;
                        merged[j] = true;
                    }
                }

                region.CenterUV = sumCenter / totalVerts;
                region.OriginalCenterUV = region.CenterUV;
                region.Name = $"Region {regionIndex + 1} ({region.VertexIndices.Count} verts)";

                _uvRegions.Add(region);
                regionIndex++;
            }

            // Sort by vertex count (largest first)
            _uvRegions = _uvRegions.OrderByDescending(r => r.VertexIndices.Count).ToList();

            // Rename and recolor after sorting
            for (int i = 0; i < _uvRegions.Count; i++)
            {
                _uvRegions[i].Name = $"Region {i + 1} ({_uvRegions[i].VertexIndices.Count} verts)";
                _uvRegions[i].DisplayColor = RegionColors[i % RegionColors.Length];
            }

            if (_uvRegions.Count > 0)
            {
                _selectedRegionIndex = 0;
            }

            Debug.Log($"[GradientColorEditor] Found {islands.Count} vertex islands, grouped into {_uvRegions.Count} UV regions");
        }

        private List<HashSet<int>> FindConnectedIslands(int vertexCount, int[] triangles)
        {
            // Build adjacency
            HashSet<int>[] adjacency = new HashSet<int>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                adjacency[i] = new HashSet<int>();
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                adjacency[v0].Add(v1);
                adjacency[v0].Add(v2);
                adjacency[v1].Add(v0);
                adjacency[v1].Add(v2);
                adjacency[v2].Add(v0);
                adjacency[v2].Add(v1);
            }

            // Flood fill to find islands
            List<HashSet<int>> islands = new List<HashSet<int>>();
            bool[] visited = new bool[vertexCount];

            for (int start = 0; start < vertexCount; start++)
            {
                if (visited[start]) continue;

                HashSet<int> island = new HashSet<int>();
                Queue<int> queue = new Queue<int>();
                queue.Enqueue(start);
                visited[start] = true;

                while (queue.Count > 0)
                {
                    int v = queue.Dequeue();
                    island.Add(v);

                    foreach (int neighbor in adjacency[v])
                    {
                        if (!visited[neighbor])
                        {
                            visited[neighbor] = true;
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                islands.Add(island);
            }

            return islands;
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Gradient Color Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Target selection
            EditorGUI.BeginChangeCheck();
            MeshRenderer newRenderer = (MeshRenderer)EditorGUILayout.ObjectField(
                "Target Renderer",
                _targetRenderer,
                typeof(MeshRenderer),
                true
            );
            if (EditorGUI.EndChangeCheck() && newRenderer != _targetRenderer)
            {
                RevertPreview();
                CleanupPreviewMesh();
                _targetRenderer = newRenderer;
                _targetMeshFilter = newRenderer?.GetComponent<MeshFilter>();
                if (_targetRenderer != null && _targetMeshFilter != null)
                {
                    SetupForMesh();
                }
            }

            if (_targetRenderer == null || _targetMeshFilter == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject with MeshRenderer and MeshFilter to edit its gradient colors.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (_originalMesh == null)
            {
                EditorGUILayout.HelpBox("No mesh found on the selected object.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (_gradientTexture == null)
            {
                string matInfo = _material != null
                    ? $"Material: {_material.name}\nShader: {_material.shader?.name ?? "None"}"
                    : "No material found";

                EditorGUILayout.HelpBox(
                    $"No gradient texture found on material.\n\n{matInfo}\n\n" +
                    "Make sure the material has a texture assigned.",
                    MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Mesh: {_originalMesh.name}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Material: {_material?.name ?? "None"} ({_material?.shader?.name ?? "No shader"})", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Texture: {_gradientTexture.name}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"UV Regions Found: {_uvRegions.Count}", EditorStyles.miniLabel);

            if (_isPreviewing)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Preview mode active. Save or Revert to finish.", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // Draw gradient and 3D preview side by side, centered
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Draw gradient with all regions marked (left)
            DrawGradientWithRegions();

            GUILayout.Space(20);

            // Draw 3D model preview (right, bigger)
            DrawModelPreview();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Region selection and editing
            if (_uvRegions.Count > 0)
            {
                DrawRegionSelector();

                if (_selectedRegionIndex >= 0 && _selectedRegionIndex < _uvRegions.Count)
                {
                    DrawRegionEditor();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No UV regions found in mesh.", MessageType.Warning);
            }

            EditorGUILayout.Space(15);

            // Save section
            DrawSaveSection();

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();
        }

        private void DrawModelPreview()
        {
            EditorGUILayout.BeginVertical();

            InitPreviewRenderUtility();

            // Get the mesh to render (preview or original)
            Mesh meshToRender = _isPreviewing && _previewMesh != null ? _previewMesh : _originalMesh;

            if (meshToRender == null || _material == null)
            {
                GUILayout.Box("No preview", GUILayout.Width(PreviewSize), GUILayout.Height(PreviewSize));
                EditorGUILayout.EndVertical();
                return;
            }

            // Create preview rect
            Rect previewRect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.ExpandWidth(false));

            // Handle mouse input for rotation
            Event e = Event.current;
            if (previewRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    _previewRotation.x += e.delta.y * 0.5f;
                    _previewRotation.y += e.delta.x * 0.5f;
                    _previewRotation.x = Mathf.Clamp(_previewRotation.x, -89f, 89f);
                    e.Use();
                    Repaint();
                }
                else if (e.type == EventType.ScrollWheel)
                {
                    _previewZoom += e.delta.y * 0.05f;
                    _previewZoom = Mathf.Clamp(_previewZoom, 0.5f, 3f);
                    e.Use();
                    Repaint();
                }
            }

            // Render preview
            _previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);

            // Setup camera
            Bounds bounds = meshToRender.bounds;
            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            float distance = maxExtent * 3f * _previewZoom;

            Quaternion rotation = Quaternion.Euler(_previewRotation.x, _previewRotation.y, 0);
            Vector3 cameraPos = bounds.center + rotation * Vector3.forward * distance;

            _previewRenderUtility.camera.transform.position = cameraPos;
            _previewRenderUtility.camera.transform.LookAt(bounds.center);
            _previewRenderUtility.camera.nearClipPlane = 0.01f;
            _previewRenderUtility.camera.farClipPlane = distance * 3f;

            // Setup lighting - make it bright like Unity's preview
            _previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(40, 40, 0);
            _previewRenderUtility.lights[0].intensity = 1.4f;
            _previewRenderUtility.lights[0].color = Color.white;

            // Add second light for fill
            if (_previewRenderUtility.lights.Length > 1)
            {
                _previewRenderUtility.lights[1].transform.rotation = Quaternion.Euler(-20, -120, 0);
                _previewRenderUtility.lights[1].intensity = 1f;
                _previewRenderUtility.lights[1].color = new Color(0.9f, 0.9f, 1f);
            }

            // Set ambient light
            _previewRenderUtility.ambientColor = new Color(0.4f, 0.4f, 0.4f, 1f);

            // Draw mesh with material
            _previewRenderUtility.DrawMesh(meshToRender, Matrix4x4.identity, _material, 0);

            // Render and end
            _previewRenderUtility.camera.Render();
            Texture resultTexture = _previewRenderUtility.EndPreview();

            // Draw the preview
            GUI.DrawTexture(previewRect, resultTexture, ScaleMode.StretchToFill, false);

            // Draw border
            Handles.BeginGUI();
            Handles.color = Color.gray;
            Handles.DrawWireCube(
                new Vector3(previewRect.center.x, previewRect.center.y, 0),
                new Vector3(previewRect.width, previewRect.height, 0)
            );
            Handles.EndGUI();

            // Label below preview
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Drag to rotate, scroll to zoom", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(PreviewSize));

            EditorGUILayout.EndVertical();
        }

        private void DrawGradientWithRegions()
        {
            EditorGUILayout.BeginVertical();

            // Reserve rect for the gradient
            Rect gradientRect = GUILayoutUtility.GetRect(GradientSize, GradientSize, GUILayout.ExpandWidth(false));

            // Draw the gradient texture
            GUI.DrawTexture(gradientRect, _gradientTexture, ScaleMode.StretchToFill);

            // Draw border
            Handles.BeginGUI();
            Handles.color = Color.gray;
            Vector3[] corners = new Vector3[]
            {
                new Vector3(gradientRect.xMin, gradientRect.yMin, 0),
                new Vector3(gradientRect.xMax, gradientRect.yMin, 0),
                new Vector3(gradientRect.xMax, gradientRect.yMax, 0),
                new Vector3(gradientRect.xMin, gradientRect.yMax, 0),
                new Vector3(gradientRect.xMin, gradientRect.yMin, 0)
            };
            Handles.DrawPolyLine(corners);

            // Draw markers for each region
            for (int i = 0; i < _uvRegions.Count; i++)
            {
                UVRegion region = _uvRegions[i];
                bool isSelected = (i == _selectedRegionIndex);

                // Convert UV to screen position (UV Y is flipped in GUI)
                float markerX = gradientRect.x + Mathf.Clamp01(region.CenterUV.x) * GradientSize;
                float markerY = gradientRect.y + (1f - Mathf.Clamp01(region.CenterUV.y)) * GradientSize;

                // Draw marker
                float size = isSelected ? MarkerSize * 1.4f : MarkerSize;
                Handles.color = region.DisplayColor;
                Handles.DrawSolidDisc(new Vector3(markerX, markerY, 0), Vector3.forward, size / 2f);

                // Draw outline
                Handles.color = isSelected ? Color.white : Color.black;
                Handles.DrawWireDisc(new Vector3(markerX, markerY, 0), Vector3.forward, size / 2f);

                if (isSelected)
                {
                    Handles.color = Color.white;
                    Handles.DrawWireDisc(new Vector3(markerX, markerY, 0), Vector3.forward, size / 2f + 2);
                }
            }

            Handles.EndGUI();

            // Handle clicks on the gradient
            Event e = Event.current;
            if (gradientRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown)
                {
                    // Check if clicking on/near an existing region marker
                    int clickedRegion = GetRegionAtPosition(e.mousePosition, gradientRect);

                    if (clickedRegion >= 0)
                    {
                        // Select the clicked region
                        _selectedRegionIndex = clickedRegion;
                        e.Use();
                        Repaint();
                    }
                    else if (_selectedRegionIndex >= 0 && _selectedRegionIndex < _uvRegions.Count)
                    {
                        // Move selected region to clicked position
                        float clickX = (e.mousePosition.x - gradientRect.x) / GradientSize;
                        float clickY = 1f - (e.mousePosition.y - gradientRect.y) / GradientSize;
                        MoveRegionTo(_selectedRegionIndex, new Vector2(Mathf.Clamp01(clickX), Mathf.Clamp01(clickY)));
                        e.Use();
                    }
                }
                else if (e.type == EventType.MouseDrag && _selectedRegionIndex >= 0 && _selectedRegionIndex < _uvRegions.Count)
                {
                    // Drag to move selected region
                    float clickX = (e.mousePosition.x - gradientRect.x) / GradientSize;
                    float clickY = 1f - (e.mousePosition.y - gradientRect.y) / GradientSize;
                    MoveRegionTo(_selectedRegionIndex, new Vector2(Mathf.Clamp01(clickX), Mathf.Clamp01(clickY)));
                    e.Use();
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Click/drag to pick color", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(GradientSize));

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Returns the index of the region at the given mouse position, or -1 if none.
        /// </summary>
        private int GetRegionAtPosition(Vector2 mousePos, Rect gradientRect)
        {
            float clickRadius = MarkerSize * 1.5f; // Detection radius

            for (int i = 0; i < _uvRegions.Count; i++)
            {
                UVRegion region = _uvRegions[i];

                // Convert UV to screen position
                float markerX = gradientRect.x + Mathf.Clamp01(region.CenterUV.x) * GradientSize;
                float markerY = gradientRect.y + (1f - Mathf.Clamp01(region.CenterUV.y)) * GradientSize;

                Vector2 markerPos = new Vector2(markerX, markerY);
                float distance = Vector2.Distance(mousePos, markerPos);

                if (distance <= clickRadius)
                {
                    return i;
                }
            }

            return -1;
        }

        private void DrawRegionSelector()
        {
            EditorGUILayout.LabelField("UV Regions", EditorStyles.boldLabel);

            for (int i = 0; i < _uvRegions.Count; i++)
            {
                UVRegion region = _uvRegions[i];
                bool isSelected = (i == _selectedRegionIndex);

                EditorGUILayout.BeginHorizontal();

                // Color indicator
                Rect colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
                EditorGUI.DrawRect(colorRect, region.DisplayColor);

                // Selection toggle
                bool newSelected = GUILayout.Toggle(isSelected, region.Name, isSelected ? EditorStyles.boldLabel : EditorStyles.label);
                if (newSelected && !isSelected)
                {
                    _selectedRegionIndex = i;
                }

                // UV position
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"({region.CenterUV.x:F2}, {region.CenterUV.y:F2})", EditorStyles.miniLabel, GUILayout.Width(80));

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawRegionEditor()
        {
            UVRegion region = _uvRegions[_selectedRegionIndex];

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Edit: {region.Name}", EditorStyles.boldLabel);

            // Manual UV sliders
            EditorGUI.BeginChangeCheck();
            float newX = EditorGUILayout.Slider("Position X", region.CenterUV.x, 0f, 1f);
            float newY = EditorGUILayout.Slider("Position Y", region.CenterUV.y, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                MoveRegionTo(_selectedRegionIndex, new Vector2(newX, newY));
            }

            EditorGUILayout.Space(5);

            // Quick position buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Top-Left"))
                MoveRegionTo(_selectedRegionIndex, new Vector2(0.1f, 0.9f));
            if (GUILayout.Button("Top-Right"))
                MoveRegionTo(_selectedRegionIndex, new Vector2(0.9f, 0.9f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Bottom-Left"))
                MoveRegionTo(_selectedRegionIndex, new Vector2(0.1f, 0.1f));
            if (GUILayout.Button("Bottom-Right"))
                MoveRegionTo(_selectedRegionIndex, new Vector2(0.9f, 0.1f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Center"))
                MoveRegionTo(_selectedRegionIndex, new Vector2(0.5f, 0.5f));
            EditorGUILayout.EndHorizontal();
        }

        private void MoveRegionTo(int regionIndex, Vector2 newCenter)
        {
            if (regionIndex < 0 || regionIndex >= _uvRegions.Count) return;
            if (_originalMesh == null) return;

            UVRegion region = _uvRegions[regionIndex];
            Vector2 delta = newCenter - region.CenterUV;

            if (delta.sqrMagnitude < 0.0001f) return;

            // Create preview mesh if not exists
            if (_previewMesh == null)
            {
                _previewMesh = CreateMeshCopy(_originalMesh);
                if (_previewMesh == null)
                {
                    Debug.LogError("[GradientColorEditor] Failed to create preview mesh");
                    return;
                }
            }

            // Get UVs from preview mesh (it's always readable since we created it)
            Vector2[] uvs = _previewMesh.uv;

            if (uvs == null || uvs.Length == 0)
            {
                Debug.LogError("[GradientColorEditor] Failed to get UVs from preview mesh");
                return;
            }

            // Move all vertices in this region
            foreach (int vertexIndex in region.VertexIndices)
            {
                if (vertexIndex < uvs.Length)
                {
                    uvs[vertexIndex] += delta;
                }
            }

            // Apply to preview mesh
            _previewMesh.uv = uvs;

            // Force mesh to update by recalculating bounds
            _previewMesh.RecalculateBounds();

            // Show preview - use mesh property (not sharedMesh) to ensure instance is used
            _targetMeshFilter.mesh = _previewMesh;
            _isPreviewing = true;

            // Update region data
            region.CenterUV = newCenter;

            // Force visual update
            EditorUtility.SetDirty(_targetMeshFilter);
            EditorUtility.SetDirty(_targetMeshFilter.gameObject);
            if (_targetRenderer != null)
            {
                EditorUtility.SetDirty(_targetRenderer);
            }

            // Force scene views to repaint
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            Repaint();
        }

        /// <summary>
        /// Creates a readable copy of a mesh, even if the source mesh is not readable.
        /// </summary>
        private Mesh CreateMeshCopy(Mesh sourceMesh)
        {
            if (sourceMesh == null) return null;

            Mesh newMesh = new Mesh();
            newMesh.name = sourceMesh.name + "_Preview";

            try
            {
                if (sourceMesh.isReadable)
                {
                    // Direct copy for readable meshes
                    newMesh.vertices = sourceMesh.vertices;
                    newMesh.triangles = sourceMesh.triangles;
                    newMesh.normals = sourceMesh.normals;
                    newMesh.tangents = sourceMesh.tangents;
                    newMesh.uv = sourceMesh.uv;
                    newMesh.uv2 = sourceMesh.uv2;
                    newMesh.colors = sourceMesh.colors;
                    newMesh.colors32 = sourceMesh.colors32;
                    newMesh.bounds = sourceMesh.bounds;
                    newMesh.subMeshCount = sourceMesh.subMeshCount;
                    for (int i = 0; i < sourceMesh.subMeshCount; i++)
                    {
                        newMesh.SetSubMesh(i, sourceMesh.GetSubMesh(i));
                    }
                }
                else
                {
                    // Use MeshDataArray for non-readable meshes
                    using (var dataArray = Mesh.AcquireReadOnlyMeshData(sourceMesh))
                    {
                        var data = dataArray[0];
                        int vertexCount = data.vertexCount;

                        // Get vertices
                        var vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
                        data.GetVertices(vertices);
                        newMesh.vertices = vertices.ToArray();
                        vertices.Dispose();

                        // Get normals if available
                        if (data.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal))
                        {
                            var normals = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
                            data.GetNormals(normals);
                            newMesh.normals = normals.ToArray();
                            normals.Dispose();
                        }

                        // Get tangents if available
                        if (data.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
                        {
                            var tangents = new NativeArray<Vector4>(vertexCount, Allocator.Temp);
                            data.GetTangents(tangents);
                            newMesh.tangents = tangents.ToArray();
                            tangents.Dispose();
                        }

                        // Get UVs
                        if (data.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))
                        {
                            var uvs = new NativeArray<Vector2>(vertexCount, Allocator.Temp);
                            data.GetUVs(0, uvs);
                            newMesh.uv = uvs.ToArray();
                            uvs.Dispose();
                        }

                        // Get UV2 if available
                        if (data.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1))
                        {
                            var uv2s = new NativeArray<Vector2>(vertexCount, Allocator.Temp);
                            data.GetUVs(1, uv2s);
                            newMesh.uv2 = uv2s.ToArray();
                            uv2s.Dispose();
                        }

                        // Get indices for each submesh
                        newMesh.subMeshCount = data.subMeshCount;
                        for (int i = 0; i < data.subMeshCount; i++)
                        {
                            var subMesh = data.GetSubMesh(i);
                            var indices = new NativeArray<int>((int)subMesh.indexCount, Allocator.Temp);
                            data.GetIndices(indices, i);
                            newMesh.SetTriangles(indices.ToArray(), i);
                            indices.Dispose();
                        }

                        newMesh.bounds = sourceMesh.bounds;
                    }
                }

                newMesh.MarkDynamic();
                return newMesh;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GradientColorEditor] Failed to copy mesh: {e.Message}");
                DestroyImmediate(newMesh);
                return null;
            }
        }

        private void DrawSaveSection()
        {
            EditorGUILayout.LabelField("Save Changes", EditorStyles.boldLabel);

            _meshName = EditorGUILayout.TextField("Prefab Name", _meshName);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _isPreviewing;
            if (GUILayout.Button("Save as Prefab", GUILayout.Height(30)))
            {
                SaveAsPrefab();
            }

            if (GUILayout.Button("Revert", GUILayout.Height(30)))
            {
                RevertPreview();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Creates a prefab with mesh, renderer, and material.\n" +
                $"Path: {PrefabFolder}/{_meshName}.prefab",
                MessageType.None
            );
        }

        private void SaveAsPrefab()
        {
            if (!_isPreviewing || _previewMesh == null)
            {
                EditorUtility.DisplayDialog("Error", "No changes to save.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(_meshName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a prefab name.", "OK");
                return;
            }

            // Ensure folders exist
            EnsureFolderExists(PrefabFolder);
            EnsureFolderExists(MeshFolder);

            string meshPath = $"{MeshFolder}/{_meshName}_Mesh.asset";
            string prefabPath = $"{PrefabFolder}/{_meshName}.prefab";

            // Check if prefab already exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                if (!EditorUtility.DisplayDialog("Overwrite?",
                    $"Prefab '{_meshName}' already exists. Overwrite?", "Yes", "No"))
                {
                    return;
                }
                AssetDatabase.DeleteAsset(prefabPath);
                AssetDatabase.DeleteAsset(meshPath);
            }

            // Save the mesh asset first
            Mesh savedMesh = Instantiate(_previewMesh);
            savedMesh.name = _meshName + "_Mesh";
            AssetDatabase.CreateAsset(savedMesh, meshPath);

            // Create a new GameObject for the prefab
            GameObject prefabObject = new GameObject(_meshName);

            // Add MeshFilter with the saved mesh
            MeshFilter meshFilter = prefabObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = savedMesh;

            // Add MeshRenderer with the material
            MeshRenderer meshRenderer = prefabObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _material;

            // Save as prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabObject, prefabPath);

            // Cleanup temporary object
            DestroyImmediate(prefabObject);

            AssetDatabase.SaveAssets();

            Debug.Log($"[GradientColorEditor] Saved prefab: {prefabPath}");

            // Revert to original and cleanup
            RevertPreview();

            EditorUtility.DisplayDialog("Success",
                $"Prefab saved to:\n{prefabPath}\n\nMesh saved to:\n{meshPath}",
                "OK");

            EditorGUIUtility.PingObject(savedPrefab);
        }

        private void RevertPreview()
        {
            if (_isPreviewing && _targetMeshFilter != null && _originalMesh != null)
            {
                _targetMeshFilter.sharedMesh = _originalMesh;
                EditorUtility.SetDirty(_targetMeshFilter);
                EditorUtility.SetDirty(_targetMeshFilter.gameObject);
            }

            _isPreviewing = false;

            // Reset region positions to original
            foreach (var region in _uvRegions)
            {
                region.CenterUV = region.OriginalCenterUV;
            }

            CleanupPreviewMesh();
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            Repaint();
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            var parts = folderPath.Split('/');
            var currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                var nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = nextPath;
            }
        }
    }
}
