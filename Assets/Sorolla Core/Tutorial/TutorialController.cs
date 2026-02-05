using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sorolla.LevelFlow;
using UnityEngine;

namespace Sorolla.Tutorial
{
    public class TutorialController : SorollaManager
    {
        /// <summary>
        /// Persistence service for storing tutorial progress. Resolved from ServiceLocator if not set.
        /// </summary>
        public IPersistenceService PersistenceService { get; set; }

        // Completion events
        public static event Action<string> OnCompleteStepRequested;
        public static event Action OnCompleteManualRequested;
        public static event Action<string> OnGateTriggered;

        /// <summary>
        /// Fired when tutorial state changes. Parameters: (currentLevel, currentStepInLevel)
        /// Used by TutorialObjectsHider to show/hide objects.
        /// </summary>
        public static event Action<int, int> OnTutorialStepChanged;

        /// <summary>
        /// Fired when a tutorial step actually enters (after entry delay). Parameters: (currentLevel, currentStepInLevel, stepId)
        /// </summary>
        public static event Action<int, int, string> OnTutorialStepEntered;

        public static void CompleteStep(string stepId) => OnCompleteStepRequested?.Invoke(stepId);
        public static void Complete() => OnCompleteManualRequested?.Invoke();
        public static void TriggerGate(string stepId) => OnGateTriggered?.Invoke(stepId);

        [Header("Configuration")]
        [SerializeField] private TutorialConfig _config;
        [SerializeField] private bool _runOnStart = true;
        [SerializeField] private TutorialArrowController _arrow;

        private GameObject _currentPanel;
        private Transform PanelParent => Sorolla.UI.UIManager.Instance?.PanelsParent;
        private ILevelFlowManager _levelFlowManager;
        private bool _subscribedToLevelFlow;

        public bool IsRunning { get; private set; }
        public int CurrentLevel => _currentLevel;
        public int CurrentStepInLevel => _currentStepInLevel;

        public const string CompletedLevelsSaveKey = "tutorial_completed_levels";

        private Dictionary<int, List<TutorialStepBase>> _levelSteps = new();
        private HashSet<int> _completedLevels = new();
        private int _currentLevel = -1;
        private int _currentStepInLevel = -1;

        private Coroutine _entryDelayRoutine;
        private Coroutine _autoCompleteRoutine;
        private bool _isPaused;
        private bool _isPlayerFreeze;
        private bool _currentStepEntered;

        // Optional hooks
        public Action<bool> SetGameplayPaused;
        public Action<bool> SetFreezePlayer;

        #region Persistence

        private HashSet<int> LoadCompletedLevels()
        {
            var service = PersistenceService ?? TryResolvePersistenceService();
            var saved = service?.LoadString(CompletedLevelsSaveKey, "") ?? "";

            var result = new HashSet<int>();
            if (string.IsNullOrEmpty(saved)) return result;

            foreach (var part in saved.Split(','))
            {
                if (int.TryParse(part.Trim(), out int level))
                    result.Add(level);
            }
            return result;
        }

        private void SaveCompletedLevels()
        {
            var service = PersistenceService ?? TryResolvePersistenceService();
            if (service == null) return;

            var csv = string.Join(",", _completedLevels.OrderBy(x => x));
            service.SaveString(CompletedLevelsSaveKey, csv);
            service.Save();
        }

        private IPersistenceService TryResolvePersistenceService()
        {
            PersistenceService = ServiceLocator.Instance?.TryResolve<IPersistenceService>();
            return PersistenceService;
        }

        #endregion

        #region Initialization

        protected override void Initialize()
        {
            if (_arrow != null)
                _arrow.gameObject.SetActive(false);

            if (_config != null)
                _levelSteps = _config.ToDictionary();

            _completedLevels = LoadCompletedLevels();

            // Subscribe to level flow events
            // LevelFlowManager is guaranteed to be available because GameManager
            // initializes managers in order (init scene) or via InitializeSceneServices (game scene)
            SubscribeToLevelFlow();
        }

        private void SubscribeToLevelFlow()
        {
            if (_subscribedToLevelFlow) return;

            _levelFlowManager = ServiceLocator.Instance?.TryResolve<ILevelFlowManager>();
            if (_levelFlowManager != null)
            {
                _levelFlowManager.OnLevelStarted += NotifyLevelPlay;
                _subscribedToLevelFlow = true;
            }
        }

        public void BuildTutorial()
        {
            var hiders = FindObjectsByType<TutorialObjectsHider>(FindObjectsSortMode.None);
            foreach (var hider in hiders)
            {
                hider.Init();
            }
        }

        void Awake()
        {
            OnCompleteStepRequested += HandleCompleteStepRequested;
            OnCompleteManualRequested += HandleCompleteManualRequested;
            OnGateTriggered += HandleGateTriggered;
        }

        void OnDisable()
        {
            StopAllStepCoroutines();
            OnCompleteStepRequested -= HandleCompleteStepRequested;
            OnCompleteManualRequested -= HandleCompleteManualRequested;
            OnGateTriggered -= HandleGateTriggered;

            if (_levelFlowManager != null)
                _levelFlowManager.OnLevelStarted -= NotifyLevelPlay;
        }

        public override void Teardown()
        {
            base.Teardown();
            StopLevelTutorial();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Call this when a level starts. Starts the tutorial for that level if not already completed.
        /// </summary>
        public void NotifyLevelPlay(int levelIndex)
        {
            Debug.Log($"[TutorialController] NotifyLevelPlay called for level {levelIndex}");

            // Always update current level and fire event (for TutorialObjectsHider)
            _currentLevel = levelIndex;
            _currentStepInLevel = 0;
            OnTutorialStepChanged?.Invoke(_currentLevel, _currentStepInLevel);

            // Check if this level has a tutorial and hasn't been completed
            if (!_levelSteps.TryGetValue(levelIndex, out var steps) || steps.Count == 0)
            {
                Debug.Log($"[TutorialController] No steps found for level {levelIndex}");
                return;
            }

            if (_completedLevels.Contains(levelIndex))
            {
                Debug.Log($"[TutorialController] Level {levelIndex} tutorial already completed");
                return;
            }

            // Start tutorial for this level
            if (_runOnStart)
                StartLevelTutorial(levelIndex);
        }

        /// <summary>
        /// Manually configure level steps at runtime (alternative to using TutorialConfig asset).
        /// </summary>
        public void ConfigureLevelSteps(Dictionary<int, List<TutorialStepBase>> levelSteps)
        {
            _levelSteps = levelSteps ?? new Dictionary<int, List<TutorialStepBase>>();
        }

        /// <summary>
        /// Resets all tutorial progress.
        /// </summary>
        public void ResetTutorial()
        {
            _completedLevels.Clear();
            SaveCompletedLevels();
        }

        /// <summary>
        /// Checks if the tutorial for a specific level has been completed.
        /// </summary>
        public bool IsLevelTutorialCompleted(int levelIndex) => _completedLevels.Contains(levelIndex);

        #endregion

        #region Tutorial Flow

        private void StartLevelTutorial(int levelIndex)
        {
            if (!_levelSteps.TryGetValue(levelIndex, out var steps) || steps.Count == 0)
                return;

            _currentLevel = levelIndex;
            _currentStepInLevel = 0;
            IsRunning = true;

            RunCurrentStep();
        }

        private void StopLevelTutorial()
        {
            IsRunning = false;
            CleanupUI();
            StopAllStepCoroutines();
        }

        private void RunCurrentStep()
        {
            if (!IsRunning) return;

            if (!_levelSteps.TryGetValue(_currentLevel, out var steps))
            {
                StopLevelTutorial();
                return;
            }

            if (_currentStepInLevel >= steps.Count)
            {
                // All steps in this level completed
                CompleteLevelTutorial();
                return;
            }

            var step = steps[_currentStepInLevel];
            _currentStepEntered = false;

            if (step.EntryMode == TutorialStepEntryMode.Gate)
            {
                Debug.Log($"[TutorialController] Step '{step.Id}' waiting for gate trigger");
            }
            else
            {
                // Trigger entry immediately
                TriggerStepEntry(step);
            }
        }

        private void CompleteLevelTutorial()
        {
            _completedLevels.Add(_currentLevel);
            SaveCompletedLevels();
            StopLevelTutorial();
        }

        private void TriggerStepEntry(TutorialStepBase step)
        {
            if (_currentStepEntered) return;

            float entryDelay = step.GetEntryDelay();
            Debug.Log($"[TutorialController] TriggerStepEntry for '{step.Id}', EntryDelay: {entryDelay}");

            if (entryDelay > 0)
            {
                StopAllStepCoroutines();
                _entryDelayRoutine = StartCoroutine(DelayedEntryRoutine(step, entryDelay));
            }
            else
            {
                EnterStep(step);
            }
        }

        private IEnumerator DelayedEntryRoutine(TutorialStepBase step, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _entryDelayRoutine = null;
            EnterStep(step);
        }

        private void EnterStep(TutorialStepBase step)
        {
            if (_currentStepEntered) return;
            _currentStepEntered = true;

            Debug.Log($"[TutorialController] Entering step: {step.Id}");

            // Fire entered event (after entry delay has completed)
            OnTutorialStepEntered?.Invoke(_currentLevel, _currentStepInLevel, step.Id);

            // Instantiate step panel
            if (step.PanelPrefab != null && PanelParent != null)
            {
                _currentPanel = Instantiate(step.PanelPrefab, PanelParent);
                Debug.Log($"[TutorialController] Panel instantiated: {_currentPanel.name}");
            }
            else if (step.PanelPrefab == null)
            {
                Debug.LogWarning($"[TutorialController] Step '{step.Id}' has no PanelPrefab assigned.");
            }
            else if (PanelParent == null)
            {
                Debug.LogWarning("[TutorialController] PanelParent is null. Is UIManager available?");
            }

            // Show/hide arrow
            if (step.ShowArrow && _arrow != null)
            {
                _arrow.Init(step);
                _arrow.gameObject.SetActive(true);
            }
            else if (_arrow != null)
            {
                _arrow.gameObject.SetActive(false);
            }

            // Set gameplay state
            SetGameplayState(step.PauseGameplayDuringStep, step.FreezePlayer);

            // Invoke enter callback
            step.OnEnter?.Invoke();

            // Start auto-complete timer if this is a Timed step
            if (step.IsAutoComplete)
            {
                float autoCompleteDelay = step.GetAutoCompleteDelay();
                if (autoCompleteDelay > 0)
                {
                    _autoCompleteRoutine = StartCoroutine(AutoCompleteRoutine(autoCompleteDelay));
                }
                else
                {
                    Advance();
                }
            }
        }

        private IEnumerator AutoCompleteRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _autoCompleteRoutine = null;
            Advance();
        }

        private void Advance()
        {
            if (!_levelSteps.TryGetValue(_currentLevel, out var steps))
                return;

            if (_currentStepInLevel >= steps.Count)
                return;

            var step = steps[_currentStepInLevel];
            step.OnExit?.Invoke();

            SetGameplayState(false, false);
            CleanupUI();
            StopAllStepCoroutines();

            _currentStepInLevel++;
            OnTutorialStepChanged?.Invoke(_currentLevel, _currentStepInLevel);

            RunCurrentStep();
        }

        #endregion

        #region Event Handlers

        private void HandleCompleteRequested(string stepId, bool isManual)
        {
            if (!IsRunning) return;

            if (!_levelSteps.TryGetValue(_currentLevel, out var steps))
                return;

            if (_currentStepInLevel >= steps.Count)
                return;

            var step = steps[_currentStepInLevel];
            if (step.EntryMode == TutorialStepEntryMode.Gate && !_currentStepEntered) return;

            bool canComplete = isManual ? step.CanCompleteManually() : step.CanCompleteByEvent(stepId);
            if (canComplete)
                Advance();
        }

        private void HandleCompleteStepRequested(string stepId) => HandleCompleteRequested(stepId, false);
        private void HandleCompleteManualRequested() => HandleCompleteRequested(null, true);

        private void HandleGateTriggered(string stepId)
        {
            if (!IsRunning) return;

            if (!_levelSteps.TryGetValue(_currentLevel, out var steps))
                return;

            if (_currentStepInLevel >= steps.Count)
                return;

            var step = steps[_currentStepInLevel];
            if (step.EntryMode != TutorialStepEntryMode.Gate || string.IsNullOrEmpty(step.Id) || step.Id != stepId)
                return;

            if (!_currentStepEntered)
                TriggerStepEntry(step);
        }

        #endregion

        #region Helpers

        private void StopAllStepCoroutines()
        {
            if (_entryDelayRoutine != null)
            {
                StopCoroutine(_entryDelayRoutine);
                _entryDelayRoutine = null;
            }
            if (_autoCompleteRoutine != null)
            {
                StopCoroutine(_autoCompleteRoutine);
                _autoCompleteRoutine = null;
            }
        }

        private void SetGameplayState(bool pause, bool freeze)
        {
            if (_isPaused != pause)
            {
                _isPaused = pause;
                GameManager.SetPaused(pause);
                SetGameplayPaused?.Invoke(pause);
            }
            if (_isPlayerFreeze != freeze)
            {
                _isPlayerFreeze = freeze;
                SetFreezePlayer?.Invoke(freeze);
            }
        }

        private void CleanupUI()
        {
            if (_arrow != null)
                _arrow.gameObject.SetActive(false);

            if (_currentPanel != null)
            {
                Destroy(_currentPanel);
                _currentPanel = null;
            }
        }

        #endregion
    }
}
