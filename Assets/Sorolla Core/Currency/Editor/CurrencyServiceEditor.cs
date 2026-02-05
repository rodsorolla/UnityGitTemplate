using UnityEditor;
using UnityEngine;

namespace Sorolla.Currency
{
    [CustomEditor(typeof(CurrencyService))]
    public class CurrencyServiceEditor : UnityEditor.Editor
    {
        private int _customAmount = 100;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var service = (CurrencyService)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use debug tools.", MessageType.Info);
                return;
            }

            // Display current balances
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Current Balances", EditorStyles.boldLabel);

            var balances = service.GetAllBalances();
            foreach (var kvp in balances)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{kvp.Key}:", GUILayout.Width(80));
                EditorGUILayout.LabelField(kvp.Value.ToString("N0"), EditorStyles.boldLabel, GUILayout.Width(100));

                if (GUILayout.Button("+100", GUILayout.Width(50)))
                    service.Add(kvp.Key, 100);
                if (GUILayout.Button("+1K", GUILayout.Width(50)))
                    service.Add(kvp.Key, 1000);
                if (GUILayout.Button("+10K", GUILayout.Width(50)))
                    service.Add(kvp.Key, 10000);

                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
                if (GUILayout.Button("Reset", GUILayout.Width(50)))
                    service.DEBUG_SetBalance(kvp.Key, 0);
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            // Custom amount controls
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Custom Amount", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _customAmount = EditorGUILayout.IntField("Amount:", _customAmount);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"Add {_customAmount} to All"))
                service.DEBUG_AddToAll(_customAmount);
            EditorGUILayout.EndHorizontal();

            // Global actions
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Global Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Log All Balances"))
                service.DEBUG_ListAll();

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Reset All to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset Currency",
                    "Reset all currencies to default values?", "Reset", "Cancel"))
                {
                    service.DEBUG_ResetAll();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

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
