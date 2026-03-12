using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Sorolla
{
    /// <summary>
    /// Ensures Play mode starts from the Init scene, but only when editing Init or Game scenes.
    /// When editing other scenes (e.g., PhotoStudio), play mode starts from the current scene.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {
        private const string INIT_SCENE_PATH = "Assets/_Game/Scenes/Init.unity";
        private const string GAME_SCENE_PATH = "Assets/_Game/Scenes/Game.unity";

        static PlayModeStartScene()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            UpdatePlayModeStartScene();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            UpdatePlayModeStartScene();
        }

        private static void UpdatePlayModeStartScene()
        {
            var currentScenePath = SceneManager.GetActiveScene().path;

            if (currentScenePath == INIT_SCENE_PATH || currentScenePath == GAME_SCENE_PATH)
            {
                var initScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(INIT_SCENE_PATH);
                if (initScene != null)
                {
                    EditorSceneManager.playModeStartScene = initScene;
                }
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }

        [MenuItem("Tools/Clear Play Mode Start Scene")]
        private static void ClearPlayModeStartScene()
        {
            EditorSceneManager.playModeStartScene = null;
        }

        [MenuItem("Tools/Set Play Mode Start Scene to Init")]
        private static void ResetPlayModeStartScene()
        {
            var initScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(INIT_SCENE_PATH);
            if (initScene != null)
            {
                EditorSceneManager.playModeStartScene = initScene;
            }
        }
    }
}
