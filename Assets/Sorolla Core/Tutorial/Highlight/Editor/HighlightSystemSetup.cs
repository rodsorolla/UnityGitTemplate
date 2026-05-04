#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sorolla.Tutorial.Highlight.Editor
{
    /// <summary>
    /// One-shot project setup for the Sorolla Tutorial Highlight system. Safe to run
    /// multiple times — idempotent by design.
    ///
    /// 1. Appends the <c>TutorialHighlight</c> sorting layer to
    ///    <c>ProjectSettings/TagManager.asset</c> if missing.
    /// 2. Walks every <see cref="HighlightTutorialStepPanel"/> prefab and warns about
    ///    misconfigured Canvases (wrong renderMode, Default sorting layer).
    /// </summary>
    public static class HighlightSystemSetup
    {
        private const string SortingLayerName = "TutorialHighlight";
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";

        [MenuItem("Tools/Sorolla Core/Tutorial/Setup Highlight System")]
        public static void Run()
        {
            int layerChanges = EnsureSortingLayer();
            int prefabWarnings = ValidatePanelPrefabs();

            Debug.Log(
                $"[HighlightSystemSetup] Done. Sorting layer changes: {layerChanges}. Prefab warnings: {prefabWarnings}.");
        }

        private static int EnsureSortingLayer()
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (tagManagerAsset == null || tagManagerAsset.Length == 0)
            {
                Debug.LogError($"[HighlightSystemSetup] Could not load {TagManagerPath}.");
                return 0;
            }

            var tagManager = new SerializedObject(tagManagerAsset[0]);
            var sortingLayers = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayers == null || !sortingLayers.isArray)
            {
                Debug.LogError("[HighlightSystemSetup] Could not find m_SortingLayers in TagManager.");
                return 0;
            }

            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                var element = sortingLayers.GetArrayElementAtIndex(i);
                var name = element.FindPropertyRelative("name");
                if (name != null && name.stringValue == SortingLayerName)
                {
                    Debug.Log($"[HighlightSystemSetup] Sorting layer '{SortingLayerName}' already present.");
                    return 0;
                }
            }

            int insertIndex = sortingLayers.arraySize;
            sortingLayers.InsertArrayElementAtIndex(insertIndex);
            var newEntry = sortingLayers.GetArrayElementAtIndex(insertIndex);
            newEntry.FindPropertyRelative("name").stringValue = SortingLayerName;
            newEntry.FindPropertyRelative("uniqueID").intValue = GenerateUniqueId(sortingLayers);
            newEntry.FindPropertyRelative("locked").boolValue = false;

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log($"[HighlightSystemSetup] Added sorting layer '{SortingLayerName}'.");
            return 1;
        }

        private static int GenerateUniqueId(SerializedProperty sortingLayers)
        {
            // Pick a stable-ish id that doesn't collide with existing entries.
            var used = new HashSet<int>();
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                var elem = sortingLayers.GetArrayElementAtIndex(i);
                var id = elem.FindPropertyRelative("uniqueID");
                if (id != null) used.Add(id.intValue);
            }

            int candidate = unchecked((int)0xBEEFFEED);
            while (used.Contains(candidate)) candidate++;
            return candidate;
        }

        private static int ValidatePanelPrefabs()
        {
            int warnings = 0;
            var guids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                var panel = prefab.GetComponent<HighlightTutorialStepPanel>();
                if (panel == null) continue;

                var canvas = prefab.GetComponent<Canvas>();
                if (canvas == null)
                {
                    Debug.LogWarning($"[HighlightSystemSetup] '{path}' has HighlightTutorialStepPanel but no Canvas on the root.", prefab);
                    warnings++;
                    continue;
                }

                if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
                {
                    Debug.LogWarning($"[HighlightSystemSetup] '{path}' Canvas renderMode is '{canvas.renderMode}'. Prefer ScreenSpaceCamera.", prefab);
                    warnings++;
                }

                if (canvas.sortingLayerName == "Default")
                {
                    Debug.LogWarning($"[HighlightSystemSetup] '{path}' Canvas sorting layer is 'Default'. Use 'Sky' (or equivalent) so the dim sits above world sprites.", prefab);
                    warnings++;
                }
            }

            if (warnings == 0)
                Debug.Log("[HighlightSystemSetup] All HighlightTutorialStepPanel prefabs look good.");
            return warnings;
        }
    }
}
#endif
