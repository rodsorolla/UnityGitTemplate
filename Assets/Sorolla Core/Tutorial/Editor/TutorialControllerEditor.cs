using UnityEditor;
using UnityEngine;

namespace Sorolla.Tutorial
{
    [CustomEditor(typeof(TutorialController))]
    [CanEditMultipleObjects]
    internal class TutorialControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(8);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Reset Tutorial Progress", GUILayout.Height(40f)))
            {
                var controller = (TutorialController)target;
                controller.ResetTutorial();
                Debug.Log("[TutorialController] Tutorial progress reset.");
            }
            GUI.enabled = true;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to reset tutorial progress.", MessageType.Info);
            }
        }
    }
}
