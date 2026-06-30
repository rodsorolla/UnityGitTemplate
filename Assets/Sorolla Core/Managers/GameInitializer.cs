using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using PaletteApi = Sorolla.Palette.Palette;

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

        [Header("Consent gate")]
        [Tooltip("Max seconds to wait for the Palette consent flow (GDPR CMP / ATT) to resolve before revealing the game scene. Prevents trapping the user on the loading screen if consent never resolves (e.g. no network).")]
        [SerializeField] private float _consentTimeoutSeconds = 10f;

        private CancellationTokenSource _cts;
        private Scene _initScene;

        // Event fired after game scene is loaded
        public static event Func<UniTask> OnSceneLoaded;

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
            catch (Exception ex)
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

            // Fire event to notify all subscribers that scene is loaded
            if (OnSceneLoaded != null)
            {
                foreach (var handler in OnSceneLoaded.GetInvocationList())
                {
                    await ((Func<UniTask>)handler)();
                }
            }

            // Hold the reveal until the Palette consent flow (GDPR CMP / ATT) has resolved,
            // so consent dialogs surface over the PreInit loading screen instead of on the
            // first level. The game scene has already loaded behind the loading screen, so
            // this only gates when the loading screen is hidden.
            await WaitForConsentAsync(_cts.Token);

            // Notify PreInit to hide loading screen and unload
            if (PreInitLoader.Instance != null)
                PreInitLoader.Instance.OnInitializationComplete();

            // Unload Init scene (fire-and-forget)
            _ = SceneManager.UnloadSceneAsync(_initScene);
        }

        /// <summary>
        /// Waits for the Palette consent flow (GDPR CMP / ATT) to resolve before the game
        /// scene is revealed. On the MAX path <see cref="PaletteApi.IsInitialized"/> stays
        /// false for the whole consent window and flips true after the user accepts/refuses;
        /// in Editor / non-MAX builds it is already true, so this returns immediately.
        /// A safety timeout proceeds anyway if consent never resolves (e.g. no network).
        /// </summary>
        async UniTask WaitForConsentAsync(CancellationToken token)
        {
            if (PaletteApi.IsInitialized) return;

            var tcs = new UniTaskCompletionSource();
            Action onInitialized = () => tcs.TrySetResult();
            PaletteApi.OnInitialized += onInitialized;
            try
            {
                // Re-check after subscribing in case init completed during subscription (race).
                if (PaletteApi.IsInitialized) return;

                var timeout = UniTask.Delay(
                    TimeSpan.FromSeconds(_consentTimeoutSeconds),
                    ignoreTimeScale: true,
                    cancellationToken: token);

                // index 0 = consent resolved, 1 = timeout elapsed
                int finished = await UniTask.WhenAny(tcs.Task, timeout);
                if (finished != 0)
                    Debug.LogWarning($"[GameInitializer] Consent flow did not resolve within {_consentTimeoutSeconds}s; revealing game scene anyway.");
            }
            finally
            {
                PaletteApi.OnInitialized -= onInitialized;
            }
        }

        void OnDestroy()
        {
            // Clear static event to prevent delegate leaks across scene reloads
            OnSceneLoaded = null;

            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}
