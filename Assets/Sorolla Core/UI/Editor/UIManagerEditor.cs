using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Sorolla.UI.Editor
{
    /// <summary>
    /// Adds a "Panel Test" section to the <see cref="UIManager"/> inspector: pick any
    /// <see cref="UIPanelId"/> from the dropdown and open it through the real
    /// <see cref="UIManager.OpenPanelAsync"/> flow (so it registers in the panel cache
    /// and closes correctly). Play-mode only — opening goes through the live singleton.
    /// </summary>
    [CustomEditor(typeof(UIManager))]
    public class UIManagerEditor : UnityEditor.Editor
    {
        private UIPanelId _selected = UIPanelId.None;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Panel Test", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to open panels.", MessageType.Info);
                return;
            }

            _selected = (UIPanelId)EditorGUILayout.EnumPopup("Panel", _selected);

            using (new EditorGUI.DisabledScope(_selected == UIPanelId.None))
            {
                if (GUILayout.Button("Open Panel"))
                {
                    // null args: panels that require args may warn/throw — pick arg-free panels.
                    UIManager.Instance.OpenPanelAsync(_selected).Forget();
                }
            }
        }
    }
}
