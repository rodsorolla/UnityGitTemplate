using UnityEditor;
using UnityEngine;

namespace Sorolla.FTX.Editor
{
    [CustomEditor(typeof(FirstTimeExperienceService))]
    public class FirstTimeExperienceServiceEditor : UnityEditor.Editor
    {
        private Vector2 _scrollPosition;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var service = (FirstTimeExperienceService)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use debug tools.", MessageType.Info);
                return;
            }

            // Display seen keys
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Seen Keys", EditorStyles.boldLabel);

            var seenKeys = service.DEBUG_GetAllSeenKeys();
            if (seenKeys == null)
            {
                EditorGUILayout.HelpBox("Service not yet initialized.", MessageType.Info);
                return;
            }

            if (seenKeys.Count == 0)
            {
                EditorGUILayout.HelpBox("No keys have been seen yet.", MessageType.Info);
            }
            else
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(200));

                foreach (var key in seenKeys)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(key, GUILayout.ExpandWidth(true));

                    GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
                    if (GUILayout.Button("Reset", GUILayout.Width(60)))
                    {
                        service.DEBUG_ResetKey(key);
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            // Global actions
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Global Actions", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Reset All"))
            {
                if (EditorUtility.DisplayDialog("Reset FTX",
                    "Reset all first-time experience data? This will show all hints again.", "Reset", "Cancel"))
                {
                    service.DEBUG_ResetAll();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Force Save", GUILayout.Height(30)))
                service.Save();
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Force Load", GUILayout.Height(30)))
                service.Load();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Repaint to keep values updated
            Repaint();
        }
    }
}
