using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sorolla
{
    /// <summary>
    /// Lightweight loader for a pre-init scene.
    /// Shows a loading screen immediately, loads Init scene additively,
    /// waits for initialization to complete, then unloads itself.
    /// </summary>
    public class PreInitLoader : MonoBehaviour
    {
        [SerializeField] private string _initSceneName = "Init";
        [SerializeField] private GameObject _loadingScreen;

        public static PreInitLoader Instance { get; private set; }

        void Awake()
        {
            Application.targetFrameRate = 60;
            Instance = this;
            if (_loadingScreen) _loadingScreen.SetActive(true);
        }

        void Start()
        {
            // Load init scene additively - PreInit stays active with loading screen
            SceneManager.LoadSceneAsync(_initSceneName, LoadSceneMode.Additive);
        }

        /// <summary>
        /// Called by GameInitializer when initialization is complete.
        /// Hides loading screen and unloads the PreInit scene.
        /// </summary>
        public void OnInitializationComplete()
        {
            if (_loadingScreen) _loadingScreen.SetActive(false);

            // Unload this scene
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }
    }
}
