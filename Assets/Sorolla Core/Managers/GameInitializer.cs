using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sorolla
{
    /// <summary>
    /// Bootstrap component that initializes the game on startup.
    /// Place this in your Bootstrap/Init scene (Build Settings index 0).
    ///
    /// <para><b>Initialization Flow:</b></para>
    /// <list type="number">
    ///   <item>Initializes GameManager and all registered subsystems</item>
    ///   <item>Loads the target game scene additively</item>
    ///   <item>Sets the game scene as active</item>
    ///   <item>Fires <see cref="OnSceneLoaded"/> event for post-load setup</item>
    ///   <item>Notifies PreInitLoader to hide loading screen (if present)</item>
    ///   <item>Unloads the bootstrap scene</item>
    /// </list>
    ///
    /// <para><b>Setup:</b></para>
    /// <list type="bullet">
    ///   <item>Create a Bootstrap scene with this component and GameManager</item>
    ///   <item>GameManager must be in Bootstrap scene (needed before Game scene loads)</item>
    ///   <item>Set <c>_gameSceneName</c> to your main game scene name</item>
    ///   <item>Add both scenes to Build Settings (Bootstrap at index 0)</item>
    /// </list>
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("Target Game Scene (by name)")]
        [SerializeField] private string _gameSceneName;

        private CancellationTokenSource _cts;
        private Scene _initScene;

        // Event fired after game scene is loaded
        public static event Func<Task> OnSceneLoaded;

        void Awake()
        {
            Application.targetFrameRate = 60;
            _cts = new CancellationTokenSource();
            _initScene = gameObject.scene;
        }

        async void Start()
        {
            // Ensure a GameManager exists and initialize it
            var gm = GameManager.Instance;

            try
            {
                await gm.InitializeAsync(_cts.Token);
            }
            catch (System.OperationCanceledException)
            {
                Debug.LogWarning("[GameInitializer] Initialization canceled");
                return;
            }
            catch (System.SystemException ex)
            {
                Debug.LogError($"[GameInitializer] Initialization failed: {ex}");
                return;
            }

            // After GameManager finished initializing subsystems, load the game scene
            if (string.IsNullOrWhiteSpace(_gameSceneName))
            {
                Debug.LogError("[GameInitializer] Game scene name is empty. Assign it in the inspector.");
                return;
            }

            var current = SceneManager.GetActiveScene();
            if (current.name == _gameSceneName)
            {
                Debug.Log("[GameInitializer] Target game scene already active.");
                return;
            }

            // Load game scene additively to keep PreInit loading screen visible
            await SceneLoader.LoadSceneAdditiveAsync(_gameSceneName);

            // Set game scene as active
            var gameScene = SceneManager.GetSceneByName(_gameSceneName);
            if (gameScene.IsValid())
                SceneManager.SetActiveScene(gameScene);

            // Fire event to notify subscribers that scene is loaded
            if (OnSceneLoaded != null)
            {
                await OnSceneLoaded.Invoke();
            }

            // Notify PreInit to hide loading screen and unload
            if (PreInitLoader.Instance != null)
                PreInitLoader.Instance.OnInitializationComplete();

            // Unload Init scene (fire-and-forget)
            _ = SceneManager.UnloadSceneAsync(_initScene);
        }

        void OnDestroy()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}
