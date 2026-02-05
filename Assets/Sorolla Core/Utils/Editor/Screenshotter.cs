using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
///     An Editor script to automate taking screenshots for the App Store
///     at specified resolutions using a keyboard shortcut.
///     Screenshots are saved outside the Assets folder in a "Screenshots" directory,
///     organized by resolution name.
///     Uses reflection extensively and incorporates delays between captures
///     to ensure correct resolution rendering for each screenshot.
/// </summary>
public static class Screenshotter
{
    // List of target resolutions (Portrait Mode)
    // Note: Sizes are added to whatever group the Game View is currently set to
    static readonly List<ScreenshotResolution> TargetResolutions = new List<ScreenshotResolution>
    {
        // --- iPhone ---
        new ScreenshotResolution("iPhone-6.7", 1290, 2796),
        new ScreenshotResolution("iPhone-6.5", 1284, 2778),
        new ScreenshotResolution("iPhone-5.5", 1242, 2208),
        // --- iPad ---
        new ScreenshotResolution("iPad-12.9", 2048, 2732),
        new ScreenshotResolution("iPad-11", 1668, 2388)
    };

    // Keep track of screenshots taken in the current run across delayCalls
    static int screenshotsTakenThisRun;
    static int screenshotTake;

    // Shortcut Key
    [MenuItem("Tools/Take App Store Screenshots #&S")]
    public static void TakeScreenshots()
    {
        string basePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
        Debug.Log($"<color=cyan>[Screenshotter]</color> Starting screenshot capture sequence. Saving to: {basePath}");

        EditorWindow gameView = GetGameViewWindow();
        if (gameView == null)
        {
            Debug.LogError("<color=red>[Screenshotter]</color> Game View window is not open or could not be found. Please open it (Window > General > Game) and try again.");
            EditorApplication.Beep();
            return;
        }

        int originalSizeIndex = GetCurrentGameViewSizeIndex(gameView);
        GameViewSizeGroupType originalGroupType = GetCurrentGroupType();
        Debug.Log($"<color=cyan>[Screenshotter]</color> Stored original Game View size index: {originalSizeIndex} in group: {originalGroupType}");

        // Reset counter and index for this run
        screenshotsTakenThisRun = 0;
        screenshotTake = EditorPrefs.GetInt("ScreenshotTake", 0);

        // Start the sequence with the first resolution (index 0)
        // Pass necessary state to the processing method
        ProcessNextScreenshot(0, gameView, basePath, originalSizeIndex, originalGroupType);
    }

    // Recursive helper method to process one screenshot at a time, chained with delayCall
    static void ProcessNextScreenshot(int index, EditorWindow gameView, string basePath, int originalSizeIndex, GameViewSizeGroupType originalGroupType)
    {
        // --- Base case: Check if index is out of bounds ---
        if (index >= TargetResolutions.Count)
        {
            // All resolutions processed, restore original settings and finish
            RestoreOriginalSizeAndFinish(gameView, originalSizeIndex, originalGroupType);
            return;
        }

        // --- Process Current Resolution ---
        ScreenshotResolution res = TargetResolutions[index];
        Debug.Log($"<color=yellow>[Screenshotter]</color> Processing [{index + 1}/{TargetResolutions.Count}]: {res.Name} ({res.Width}x{res.Height})");

        // Get the current group type (whatever platform the Game View is set to)
        GameViewSizeGroupType currentGroup = GetCurrentGroupType();

        // Find/Add and Set Game View Size in the current group
        int sizeIndex = FindOrAddGameViewSize(currentGroup, res.Width, res.Height, res.Name);
        if (sizeIndex == -1)
        {
            Debug.LogError($"<color=red>[Screenshotter]</color> Failed to find or add GameView size for {res.Name}. Skipping.");
            // Schedule the next one immediately to keep the sequence going
            EditorApplication.delayCall += () => { ProcessNextScreenshot(index + 1, gameView, basePath, originalSizeIndex, originalGroupType); };
            return;
        }
        SetGameViewSize(gameView, sizeIndex);

        // 3. Prepare Path and Directory
        string timestamp = DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss");
        // Add index to filename for uniqueness and sorting
        string filename = $"{res.Name}_{timestamp}_{screenshotTake}.png";
        string directoryPath = Path.Combine(basePath, res.Name);
        string fullPath = Path.Combine(directoryPath, filename);

        try
        {
            Directory.CreateDirectory(directoryPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>[Screenshotter]</color> Failed to create directory '{directoryPath}': {e.Message}. Skipping.");
            // Schedule the next one
            EditorApplication.delayCall += () => { ProcessNextScreenshot(index + 1, gameView, basePath, originalSizeIndex, originalGroupType); };
            return;
        }

        // 4. Request Screenshot Capture (asynchronous)
        try
        {
            ScreenCapture.CaptureScreenshot(fullPath);
            // Log request, not completion yet
            // Debug.Log($"[Screenshotter] Capture requested for: {fullPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>[Screenshotter]</color> Failed to request screenshot for {res.Name} to '{fullPath}': {e.Message}");
            // Still schedule the next one even if capture fails to request
            EditorApplication.delayCall += () => { ProcessNextScreenshot(index + 1, gameView, basePath, originalSizeIndex, originalGroupType); };
            return;
        }

        // --- Schedule Next Step via delayCall ---
        // This is the crucial part: Wait at least one frame before processing the next resolution.
        // This gives CaptureScreenshot time to finish the capture for the *current* resolution
        // before we change the resolution again.
        EditorApplication.delayCall += () =>
        {
            // Log success *after* the delay, assuming capture completed in the previous frame.
            // Optional: Check if file exists for more certainty (can be slow if many files)
            if (File.Exists(fullPath))
            {
                Debug.Log($"<color=green>[Screenshotter]</color> Screenshot likely saved: {fullPath}");
                screenshotsTakenThisRun++; // Increment counter only if file seems saved
            }
            else
            {
                // This might happen if capture failed silently or disk issues occurred.
                Debug.LogWarning($"[Screenshotter] Screenshot file may not have been saved correctly or is not yet visible: {fullPath}");
                // Optionally, you could retry here or just log it.
            }

            // Process the next resolution in the sequence
            ProcessNextScreenshot(index + 1, gameView, basePath, originalSizeIndex, originalGroupType);
        };
    }

    // Helper method to restore original settings and log completion message
    static void RestoreOriginalSizeAndFinish(EditorWindow gameView, int originalSizeIndex, GameViewSizeGroupType originalGroupType)
    {
        Debug.Log("<color=cyan>[Screenshotter]</color> All resolutions processed. Attempting to restore original Game View size...");

        // Ensure GameView is still valid before restoring (it might have been closed)
        EditorWindow currentGameView = GetGameViewWindow(); // Get it again just in case
        if (currentGameView != null)
        {
            // Restore Size Index (group type is not changed, so no need to restore it)
            SetGameViewSize(currentGameView, originalSizeIndex);
            Debug.Log("<color=cyan>[Screenshotter]</color> Original Game View size restored.");
        }
        else
        {
            Debug.LogWarning("[Screenshotter] Could not find Game View window to restore size. Was it closed?");
        }

        Debug.Log($"<color=cyan>[Screenshotter]</color> Screenshot capture sequence finished. {screenshotsTakenThisRun} screenshots potentially saved.");
        AssetDatabase.Refresh(); // Refresh to show files in Unity (if browsing parent folder)
        EditorApplication.Beep(); // Notify user
        screenshotTake++; // Increment for next run
        EditorPrefs.SetInt("ScreenshotTake", screenshotTake); // Save incremented value
    }
    // Define the structure for screenshot resolution data
    struct ScreenshotResolution
    {
        public readonly string Name;
        public readonly int Width;
        public readonly int Height;

        public ScreenshotResolution(string name, int width, int height)
        {
            Name = name;
            Width = width;
            Height = height;
        }
    }


    #region Game View Utilities (using Reflection and Public API)

    // Gets the Assembly containing Editor types
    static Assembly GetEditorAssembly() => typeof(EditorWindow).Assembly;

    // Gets internal types using reflection safely
    static Type GetTypeFromEditorAssembly(string typeName)
    {
        Assembly assembly = GetEditorAssembly();
        Type type = assembly.GetType(typeName);
        if (type == null)
        {
            Debug.LogError($"[Screenshotter] Failed to get internal type '{typeName}' via reflection. Compatibility issue?");
        }
        return type;
    }

    // Gets the active GameView EditorWindow.
    static EditorWindow GetGameViewWindow()
    {
        Type gameViewType = GetTypeFromEditorAssembly("UnityEditor.GameView");
        if (gameViewType == null) return null;

        // Use GetWindowDontShow to find without forcing visible or focus
        EditorWindow window = EditorWindow.GetWindow(gameViewType, false, "Game", false);
        return window;
    }

    // Sets the Game View size using its index.
    static void SetGameViewSize(EditorWindow gameView, int index)
    {
        if (gameView == null) return;

        MethodInfo sizeSelectionCallback = gameView.GetType().GetMethod("SizeSelectionCallback",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (sizeSelectionCallback != null)
        {
            try
            {
                sizeSelectionCallback.Invoke(gameView, new object[] { index, null });
                gameView.Repaint(); // Request repaint after size change
            }
            catch (Exception e)
            {
                Debug.LogError($"[Screenshotter] Error invoking SizeSelectionCallback: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("[Screenshotter] Could not find 'SizeSelectionCallback' method on GameView.");
        }
    }

    // Gets the current Game View size index.
    static int GetCurrentGameViewSizeIndex(EditorWindow gameView)
    {
        if (gameView == null) return 0;

        PropertyInfo selectedSizeIndexProp = gameView.GetType().GetProperty("selectedSizeIndex",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (selectedSizeIndexProp != null)
        {
            return (int)selectedSizeIndexProp.GetValue(gameView, null);
        }
        Debug.LogWarning("[Screenshotter] Could not find 'selectedSizeIndex' property on GameView.");
        return 0;
    }

    // Finds a Game View size matching the criteria or adds it if not found.
    // Returns the index of the size, or -1 on failure.
    // Uses reflection extensively to avoid direct type references.
    static int FindOrAddGameViewSize(GameViewSizeGroupType groupType, int width, int height, string baseText)
    {
        // Get internal types needed
        Type gameViewSizeGroupType = GetTypeFromEditorAssembly("UnityEditor.GameViewSizeGroup");
        Type gameViewSizeType = GetTypeFromEditorAssembly("UnityEditor.GameViewSize");
        Type gameViewSizeTypeType = GetTypeFromEditorAssembly("UnityEditor.GameViewSizeType"); // Enum type

        if (gameViewSizeGroupType == null || gameViewSizeType == null || gameViewSizeTypeType == null)
        {
            Debug.LogError("[Screenshotter] Failed to get required internal types via reflection.");
            return -1;
        }

        // Get the specific group object (casting to object)
        object group = GetGroup(groupType); // GetGroup now returns object
        if (group == null)
        {
            Debug.LogError($"[Screenshotter] Could not get GameViewSizeGroup object for {groupType}.");
            return -1;
        }

        // Get necessary PropertyInfo and MethodInfo via reflection
        MethodInfo getTotalCountMethod = gameViewSizeGroupType.GetMethod("GetTotalCount");
        MethodInfo getGameViewSizeMethod = gameViewSizeGroupType.GetMethod("GetGameViewSize");
        MethodInfo addCustomSizeMethod = gameViewSizeGroupType.GetMethod("AddCustomSize");

        PropertyInfo sizeTypeProp = gameViewSizeType.GetProperty("sizeType");
        PropertyInfo widthProp = gameViewSizeType.GetProperty("width");
        PropertyInfo heightProp = gameViewSizeType.GetProperty("height");
        PropertyInfo baseTextProp = gameViewSizeType.GetProperty("baseText");

        // Get the enum value for FixedResolution using reflection
        object fixedResolutionEnumValue = Enum.Parse(gameViewSizeTypeType, "FixedResolution");

        // Try to find an existing size
        int count = (int)getTotalCountMethod.Invoke(group, null);
        for (int i = 0; i < count; i++)
        {
            object sizeObj = getGameViewSizeMethod.Invoke(group, new object[] { i }); // Get size as object

            // Get properties via reflection
            object currentSizeType = sizeTypeProp.GetValue(sizeObj);
            int currentWidth = (int)widthProp.GetValue(sizeObj);
            int currentHeight = (int)heightProp.GetValue(sizeObj);
            string currentBaseText = (string)baseTextProp.GetValue(sizeObj);

            // Compare properties
            if (currentSizeType.Equals(fixedResolutionEnumValue) && // Use Equals for enum comparison
                currentWidth == width &&
                currentHeight == height &&
                currentBaseText == baseText)
            {
                // Debug.Log($"[Screenshotter] Found existing GameView size '{baseText}' at index {i}.");
                return i; // Found it
            }
        }

        // Size not found, add it using reflection
        try
        {
            // Find the constructor for GameViewSize: GameViewSize(GameViewSizeType type, int width, int height, string baseText)
            ConstructorInfo constructor = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeType, typeof(int), typeof(int), typeof(string) });
            if (constructor == null)
            {
                Debug.LogError("[Screenshotter] Could not find GameViewSize constructor via reflection.");
                return -1;
            }

            // Create a new GameViewSize instance using reflection
            object newSizeObj = constructor.Invoke(new[] { fixedResolutionEnumValue, width, height, baseText });

            // Add the new size object to the group using reflection
            addCustomSizeMethod.Invoke(group, new[] { newSizeObj });

            Debug.Log($"<color=orange>[Screenshotter]</color> Added new GameView size: '{baseText}' ({width}x{height}) to group {groupType}.");

            // Return the index of the newly added size (which is the last one)
            return (int)getTotalCountMethod.Invoke(group, null) - 1;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Screenshotter] Failed to add GameView size '{baseText}' via reflection: {e.Message}\n{e.StackTrace}");
            return -1;
        }
    }

    // --- Internal GameViewSizes Handling (Requires Reflection) ---

    // Gets the internal 'GameViewSizes' singleton instance via reflection. Returns object.
    static object GetGameViewSizesInstance()
    {
        Type sizesType = GetTypeFromEditorAssembly("UnityEditor.GameViewSizes");
        if (sizesType == null) return null;

        Type singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        PropertyInfo instanceProp = singleType.GetProperty("instance");
        return instanceProp?.GetValue(null, null);
    }

    // Gets a specific GameViewSizeGroup object (e.g., for iOS) from the GameViewSizes instance. Returns object.
    static object GetGroup(GameViewSizeGroupType type) // Return type changed to object
    {
        object sizesInstance = GetGameViewSizesInstance();
        if (sizesInstance == null) return null;

        MethodInfo getGroupMethod = sizesInstance.GetType().GetMethod("GetGroup");
        if (getGroupMethod == null)
        {
            Debug.LogError("[Screenshotter] Failed to find 'GetGroup' method on GameViewSizes instance.");
            return null;
        }
        // Invoke the method, passing the enum value (which might need casting depending on the method signature)
        try
        {
            // GameViewSizeGroupType is public, so it might accept the enum directly or its int value
            return getGroupMethod.Invoke(sizesInstance, new object[] { type });
        }
        catch (Exception e)
        {
            Debug.LogError($"[Screenshotter] Error invoking GetGroup method: {e.Message}\nTrying with int value...");
            // Fallback: Try invoking with the integer value of the enum
            try
            {
                return getGroupMethod.Invoke(sizesInstance, new object[] { (int)type });
            }
            catch (Exception e2)
            {
                Debug.LogError($"[Screenshotter] Error invoking GetGroup method with int value: {e2.Message}");
                return null;
            }
        }
    }

    // Gets the currently active GameViewSizeGroupType (public enum).
    static GameViewSizeGroupType GetCurrentGroupType()
    {
        object sizesInstance = GetGameViewSizesInstance();
        if (sizesInstance == null) return default(GameViewSizeGroupType); // Return default if instance not found

        PropertyInfo currentGroupTypeProp = sizesInstance.GetType().GetProperty("currentGroupType");
        if (currentGroupTypeProp == null)
        {
            Debug.LogWarning("[Screenshotter] Could not find 'currentGroupType' property on GameViewSizes.");
            return default(GameViewSizeGroupType);
        }
        // The property itself should return the public enum type directly
        return (GameViewSizeGroupType)currentGroupTypeProp.GetValue(sizesInstance, null);
    }

    // Sets the currently active GameViewSizeGroupType (public enum).
    static void SetCurrentGroupType(GameViewSizeGroupType groupType)
    {
        object sizesInstance = GetGameViewSizesInstance();
        if (sizesInstance == null) return;

        PropertyInfo currentGroupTypeProp = sizesInstance.GetType().GetProperty("currentGroupType");
        if (currentGroupTypeProp == null)
        {
            Debug.LogWarning("[Screenshotter] Could not find 'currentGroupType' property on GameViewSizes to set.");
            return;
        }
        try
        {
            currentGroupTypeProp.SetValue(sizesInstance, groupType);
            // Debug.Log($"[Screenshotter] Set GameView group type to: {groupType}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Screenshotter] Error setting currentGroupType: {e.Message}");
        }
    }

    #endregion
}
