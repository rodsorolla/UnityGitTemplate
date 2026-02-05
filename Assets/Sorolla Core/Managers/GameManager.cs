using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sorolla.LevelFlow;
using Sorolla.PersistentData;
using Sorolla.Tutorial;
using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Central orchestrator for game-wide systems. Initializes subsystems asynchronously.
    /// Now uses ServiceLocator for dependency injection to decouple from game-specific implementations.
    /// 
    /// USAGE:
    /// - Register services: ServiceLocator.Instance.Register<IPlayerProvider>(myPlayerProvider)
    /// - Resolve services: var player = ServiceLocator.Instance.Resolve<IPlayerProvider>()
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoSingleton<GameManager>
    {
        [Header("Core Managers (Auto-registered in ServiceLocator)")]
        [SerializeField] private LevelFlowManager _levelFlowManager;
        [SerializeField] private TutorialController _tutorialController;
        [SerializeField] private AudioManager _audioManager;
        
        [Header("Game-Specific Managers (Inject via ServiceLocator)")]
        [Tooltip("Add game-specific manager references here. They will be auto-registered.")]
        [SerializeField] private MonoBehaviour[] _gameManagers;

        private Task _initializationTask;
        private bool _initialized;
        private bool _initializing;

        public bool IsInitialized => _initialized;
        public bool IsInitializing => _initializing;

        // Quick access properties for core services
        public static AudioManager Audio => Instance?._audioManager;

        #region Pause System

        private bool _isPaused;

        /// <summary>
        /// Event fired when game pause state changes. Parameter is true when paused.
        /// </summary>
        public static event Action<bool> OnPauseStateChanged;

        /// <summary>
        /// Whether the game is currently paused.
        /// </summary>
        public static bool IsPaused => Instance != null && Instance._isPaused;

        /// <summary>
        /// Pause the game. Sets Time.timeScale to 0 and fires OnPauseStateChanged.
        /// </summary>
        public static void Pause()
        {
            if (Instance == null || Instance._isPaused) return;
            Instance._isPaused = true;
            Time.timeScale = 0f;
            OnPauseStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Resume the game. Sets Time.timeScale to 1 and fires OnPauseStateChanged.
        /// </summary>
        public static void Resume()
        {
            if (Instance == null || !Instance._isPaused) return;
            Instance._isPaused = false;
            Time.timeScale = 1f;
            OnPauseStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Set game pause state.
        /// </summary>
        public static void SetPaused(bool paused)
        {
            if (paused) Pause();
            else Resume();
        }

        #endregion

        protected override void Init()
        {
            base.Init();
            
            // Register core Sorolla services
            RegisterCoreServices();
            
            // Subscribe to scene loaded event (wrapped for safe async handling)
            GameInitializer.OnSceneLoaded += HandleSceneLoadedSafe;
        }

        void OnDestroy()
        {
            GameInitializer.OnSceneLoaded -= HandleSceneLoadedSafe;
        }

        /// <summary>
        /// Safe wrapper for async event handler. Catches exceptions to prevent silent failures.
        /// </summary>
        private async Task HandleSceneLoadedSafe()
        {
            try
            {
                await HandleSceneLoaded();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameManager] HandleSceneLoaded failed: {ex}");
            }
        }

        /// <summary>
        /// Register core Sorolla services in ServiceLocator for dependency injection.
        /// Game-specific services should be registered by game managers.
        /// </summary>
        private void RegisterCoreServices()
        {
            // Register Tutorial Controller
            if (_tutorialController != null)
            {
                ServiceLocator.Instance.Register(_tutorialController);
            }
            
            // Register Audio Manager
            if (_audioManager != null)
            {
                ServiceLocator.Instance.Register(_audioManager);
            }
            
            // Register game-specific managers
            if (_gameManagers != null)
            {
                foreach (var manager in _gameManagers)
                {
                    if (manager != null)
                    {
                        ServiceLocator.Instance.Register(manager);
                    }
                }
            }
        }

        /// <summary>
        /// Called after game scene is loaded - handles post-scene-load logic.
        /// Initializes any SorollaManager components found in the newly loaded scene.
        /// Override in game-specific GameManager subclass for additional behavior.
        /// </summary>
        protected virtual Task HandleSceneLoaded()
        {
            if (!_initialized)
            {
                Debug.LogWarning("[GameManager] HandleSceneLoaded called but GameManager is not initialized.");
                return Task.CompletedTask;
            }

            // Initialize any SorollaManager components in the loaded scene
            InitializeSceneServices();

            // Start background music (AudioManager handles disabled state)
            _audioManager?.PlayMusic("Music1");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Finds and initializes all SorollaManager components in the current scene
        /// that haven't been initialized yet. Called automatically after scene load.
        /// </summary>
        protected virtual void InitializeSceneServices()
        {
            var sceneManagers = FindObjectsByType<SorollaManager>(FindObjectsSortMode.None)
                .Where(m => m != null && !m.IsInitialized)
                .ToArray();

            if (sceneManagers.Length == 0) return;

            foreach (var manager in sceneManagers)
            {
                manager.Init();
            }
        }

        /// <summary>
        /// Initializes all subsystems asynchronously. Safe to call multiple times; concurrent callers await the same task.
        /// </summary>
        public Task InitializeAsync(CancellationToken ct = default)
        {
            if (_initialized) return Task.CompletedTask;
            if (_initializationTask != null) return _initializationTask;

            _initializationTask = InitializeImplAsync(ct);
            return _initializationTask;
        }

        private async Task InitializeImplAsync(CancellationToken ct)
        {
            if (_initialized) return;
            _initializing = true;
            
            try
            {
                ct.ThrowIfCancellationRequested();
                
                // Minimal yield to avoid Awake/Start race
                await Task.Yield();
                ct.ThrowIfCancellationRequested();

                // Initialize managers that need async (scene loading, asset bundles, etc)
                await InitializeManagersAsync(ct);

                _initialized = true;
                Debug.Log("[GameManager] Initialization complete.");
            }
            catch (OperationCanceledException)
            {
                _initializationTask = null;
                Debug.LogWarning("[GameManager] Initialization canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _initializationTask = null;
                Debug.LogError($"[GameManager] Initialization failed: {ex}");
                throw;
            }
            finally
            {
                _initializing = false;
            }
        }

        /// <summary>
        /// Initialize all registered managers. Override to add custom initialization logic.
        /// Initialization order: SaveSystem → Core managers → Game managers
        /// </summary>
        protected virtual async Task InitializeManagersAsync(CancellationToken ct)
        {
            // 1. Initialize SaveSystem first - other managers depend on it for persisted data
            SaveSystem.Initialize();
            await Task.Yield(); // Allow cancellation check
            ct.ThrowIfCancellationRequested();

            // 2. Initialize core Sorolla managers (can now load saved preferences)
            // LevelFlowManager first - other managers depend on it for events
            _levelFlowManager?.Init();
            _audioManager?.Init();
            _tutorialController?.Init();
            await Task.Yield();
            ct.ThrowIfCancellationRequested();

            // 3. Initialize game-specific managers (LevelFlowManager, CurrencyService, etc.)
            if (_gameManagers != null)
            {
                foreach (var manager in _gameManagers)
                {
                    if (manager == null) continue;
                    ct.ThrowIfCancellationRequested();

                    if (manager is SorollaManager sorollaManager)
                        sorollaManager.Init();
                }
            }
        }
    }
}