using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Settings for rendering a prefab icon.
    /// </summary>
    public struct IconRenderSettings
    {
        /// <summary>Camera horizontal rotation in degrees (0 = +Z axis).</summary>
        public float Azimuth;

        /// <summary>Camera vertical angle in degrees above horizontal.</summary>
        public float Elevation;

        /// <summary>Orthographic size multiplier (1.0 = tight fit, 1.2 = 20% padding).</summary>
        public float Zoom;

        /// <summary>Background color (use alpha=0 for transparent).</summary>
        public Color BackgroundColor;

        /// <summary>Directional light pitch in degrees.</summary>
        public float LightPitch;

        /// <summary>Directional light yaw in degrees.</summary>
        public float LightYaw;

        /// <summary>Directional light intensity.</summary>
        public float LightIntensity;

        /// <summary>Directional light color.</summary>
        public Color LightColor;

        /// <summary>Output resolution in pixels (square).</summary>
        public int Resolution;

        /// <summary>Supersample factor (e.g. 4 = render at 4x then downscale for AA).</summary>
        public int SupersampleFactor;

        /// <summary>Horizontal camera pan offset (relative to object size).</summary>
        public float CameraOffsetX;

        /// <summary>Vertical camera pan offset (relative to object size).</summary>
        public float CameraOffsetY;

        /// <summary>Optional background texture rendered behind the object. Null = use BackgroundColor.</summary>
        public Texture2D BackgroundTexture;

        /// <summary>
        /// Default settings matching the original ItemDataGenerator rendering pipeline.
        /// Azimuth=45, Elevation=19.47 produces direction (1, 0.5, 1).normalized.
        /// </summary>
        public static IconRenderSettings Default => new()
        {
            Azimuth = 45f,
            Elevation = 19.47f,
            Zoom = 1.2f,
            BackgroundColor = new Color(0, 0, 0, 0),
            LightPitch = 30f,
            LightYaw = 160f,
            LightIntensity = 1f,
            LightColor = Color.white,
            Resolution = 1024,
            SupersampleFactor = 4,
            CameraOffsetX = 0f,
            CameraOffsetY = 0f,
            BackgroundTexture = null
        };
    }

    /// <summary>
    /// Static utility for rendering prefab icons with configurable camera and lighting.
    /// Extracted and parameterized from ItemDataGenerator.GenerateSpritePNG().
    /// </summary>
    public static class PrefabIconRenderer
    {
        /// <summary>
        /// Renders a prefab to a Texture2D using the given settings.
        /// Caller is responsible for destroying the returned texture when done.
        /// </summary>
        public static Texture2D RenderIcon(GameObject prefab, IconRenderSettings settings)
        {
            // Hide all scene objects so they don't appear in the render
            var sceneRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var wasActive = new bool[sceneRoots.Length];
            for (int i = 0; i < sceneRoots.Length; i++)
            {
                wasActive[i] = sceneRoots[i].activeSelf;
                sceneRoots[i].SetActive(false);
            }

            Texture2D result = null;
            GameObject tempObject = null;
            GameObject cameraGO = null;
            GameObject lightGO = null;
            RenderTexture renderTexture = null;

            try
            {
                // Instantiate prefab at origin
                tempObject = Object.Instantiate(prefab);
                tempObject.transform.position = Vector3.zero;
                tempObject.transform.rotation = Quaternion.identity;

                Bounds bounds = CalculateBounds(tempObject);
                float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

                // Camera direction from azimuth/elevation
                float azRad = settings.Azimuth * Mathf.Deg2Rad;
                float elRad = settings.Elevation * Mathf.Deg2Rad;
                Vector3 cameraDir = new Vector3(
                    Mathf.Sin(azRad) * Mathf.Cos(elRad),
                    Mathf.Sin(elRad),
                    Mathf.Cos(azRad) * Mathf.Cos(elRad)
                ).normalized;

                // Camera setup
                cameraGO = new GameObject("IconRenderCamera");
                Camera camera = cameraGO.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = settings.BackgroundColor;
                camera.orthographic = true;
                camera.orthographicSize = maxExtent * settings.Zoom;
                camera.transform.position = bounds.center + cameraDir * (maxExtent * 3f);
                camera.transform.LookAt(bounds.center);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = maxExtent * 10f;

                // Apply camera offset (pans the view without changing rotation)
                camera.transform.position += camera.transform.right * settings.CameraOffsetX * maxExtent;
                camera.transform.position += camera.transform.up * settings.CameraOffsetY * maxExtent;

                // Light setup
                lightGO = new GameObject("IconRenderLight");
                Light light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = settings.LightIntensity;
                light.color = settings.LightColor;
                light.transform.rotation = Quaternion.Euler(settings.LightPitch, settings.LightYaw, 0f);

                // Render at supersampled resolution
                int ssSize = settings.Resolution * settings.SupersampleFactor;
                renderTexture = new RenderTexture(ssSize, ssSize, 24, RenderTextureFormat.ARGB32);
                renderTexture.antiAliasing = 1;
                renderTexture.filterMode = FilterMode.Bilinear;
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.Render();

                // Read supersampled pixels
                RenderTexture.active = renderTexture;
                Texture2D hiRes = new Texture2D(ssSize, ssSize, TextureFormat.ARGB32, false);
                hiRes.ReadPixels(new Rect(0, 0, ssSize, ssSize), 0, 0);
                hiRes.Apply();
                RenderTexture.active = null;

                // Downscale to final resolution
                if (settings.SupersampleFactor > 1)
                {
                    RenderTexture downscaleRT = RenderTexture.GetTemporary(
                        settings.Resolution, settings.Resolution, 0, RenderTextureFormat.ARGB32);
                    downscaleRT.filterMode = FilterMode.Bilinear;
                    Graphics.Blit(hiRes, downscaleRT);

                    RenderTexture.active = downscaleRT;
                    result = new Texture2D(settings.Resolution, settings.Resolution, TextureFormat.ARGB32, false);
                    result.ReadPixels(new Rect(0, 0, settings.Resolution, settings.Resolution), 0, 0);
                    result.Apply();
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(downscaleRT);
                    Object.DestroyImmediate(hiRes);
                }
                else
                {
                    result = hiRes;
                }

                // Composite background image behind the rendered object
                if (result != null && settings.BackgroundTexture != null)
                {
                    int res = result.width;

                    // Scale background texture to output resolution via GPU blit
                    RenderTexture bgRT = RenderTexture.GetTemporary(res, res, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(settings.BackgroundTexture, bgRT);
                    RenderTexture.active = bgRT;
                    Texture2D bgScaled = new Texture2D(res, res, TextureFormat.ARGB32, false);
                    bgScaled.ReadPixels(new Rect(0, 0, res, res), 0, 0);
                    bgScaled.Apply();
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(bgRT);

                    // Alpha-composite: object over background
                    Color[] bgPixels = bgScaled.GetPixels();
                    Color[] objPixels = result.GetPixels();

                    for (int p = 0; p < objPixels.Length; p++)
                    {
                        float a = objPixels[p].a;
                        bgPixels[p].r = objPixels[p].r * a + bgPixels[p].r * (1f - a);
                        bgPixels[p].g = objPixels[p].g * a + bgPixels[p].g * (1f - a);
                        bgPixels[p].b = objPixels[p].b * a + bgPixels[p].b * (1f - a);
                        bgPixels[p].a = a + bgPixels[p].a * (1f - a);
                    }

                    result.SetPixels(bgPixels);
                    result.Apply();
                    Object.DestroyImmediate(bgScaled);
                }
            }
            finally
            {
                // Cleanup temporary objects
                if (tempObject != null) Object.DestroyImmediate(tempObject);
                if (cameraGO != null) Object.DestroyImmediate(cameraGO);
                if (lightGO != null) Object.DestroyImmediate(lightGO);
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }

                // Restore scene objects
                for (int i = 0; i < sceneRoots.Length; i++)
                {
                    if (sceneRoots[i] != null)
                        sceneRoots[i].SetActive(wasActive[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Saves a texture to PNG at the given full file path.
        /// Creates the directory if it doesn't exist.
        /// </summary>
        public static void SaveIconToPNG(Texture2D texture, string fullPath)
        {
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(fullPath, pngData);
        }

        /// <summary>
        /// Calculates the combined world-space bounds of all Renderers on the given GameObject.
        /// </summary>
        public static Bounds CalculateBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(obj.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        /// <summary>
        /// Configures the TextureImporter at the given asset path for sprite usage.
        /// </summary>
        public static void ConfigureSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
