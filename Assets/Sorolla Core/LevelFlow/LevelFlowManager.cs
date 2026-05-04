using System;
using Cysharp.Threading.Tasks;
using Sorolla.PersistentData;
using Sorolla.UI;
using UnityEngine;

namespace Sorolla.LevelFlow
{
    /// <summary>
    /// Core level flow management service.
    /// Handles state machine, persistence, and UI integration.
    /// Place in init scene for proper initialization order.
    /// Game-specific setup should subscribe to OnLevelSetupRequested event.
    /// </summary>
    public class LevelFlowManager : SorollaManager, ILevelFlowManager
    {
        #region Constants

        protected const string SaveFileName = "level_progress";

        #endregion

        #region Settings

        [Header("End Panel")]
        [SerializeField] private float _endPanelDelay = 1.5f;

        #endregion

        #region State

        private LevelState _currentState = LevelState.Idle;
        private LevelEndReason _lastEndReason = LevelEndReason.None;
        private int _currentLevelIndex = 1;
        private UIPanel _subscribedEndPanel;

        public LevelState CurrentState => _currentState;
        public LevelEndReason LastEndReason => _lastEndReason;
        public bool IsLevelActive => _currentState == LevelState.Playing || _currentState == LevelState.Paused;
        public int CurrentLevelIndex => _currentLevelIndex;

        #endregion

        #region Progress Data

        protected LevelProgressData _progressData;
        private bool _progressLoaded;
        private int _totalLevelCount = 1;

        public int HighestLevelReached => _progressData?.highestLevelReached ?? 1;
        public int TotalLevelCount => _totalLevelCount;

        /// <summary>
        /// Converts a progressive level index to the actual level content index (1-based).
        /// Example: With 3 levels, progressive level 4 returns actual level 1.
        /// </summary>
        public int GetActualLevelIndex(int progressiveLevelIndex)
        {
            int total = TotalLevelCount;
            if (total <= 0) return 1;
            return ((progressiveLevelIndex - 1) % total) + 1;
        }

        #endregion

        #region World System

        private WorldConfig[] _cachedWorlds;
        private int[] _worldStartLevels; // Cache of first level index for each world

        public bool UsesWorldSystem => _cachedWorlds != null && _cachedWorlds.Length > 0;
        public int CurrentWorldIndex => UsesWorldSystem ? GetWorldForLevel(_currentLevelIndex) : 1;
        public int HighestWorldReached => _progressData?.highestWorldReached ?? 1;
        public int WorldCount => UsesWorldSystem ? GetWorldConfigs().Length : 1;

        #endregion

        #region Events

        public event Action<LevelState> OnStateChanged;
        public event Action<int> OnLevelSetupRequested;
        public event Action OnLevelCleanupRequested;
        public event Action<int> OnLevelStarted;
        public event Action<LevelEndReason> OnLevelEnded;
        public event Action OnLevelPaused;
        public event Action OnLevelResumed;
        public event Action<int> OnWorldCompleted;
        public event Action<int> OnWorldUnlocked;
        public event Action OnEndPanelDismissed;

        #endregion

        #region Virtual Methods - Games Can Override

        /// <summary>
        /// Returns world configurations. Return null or empty for flat level system.
        /// </summary>
        protected virtual WorldConfig[] GetWorldConfigs() => null;

        /// <summary>
        /// Returns the panel ID to show when level is won.
        /// </summary>
        protected virtual UIPanelId GetWinPanelId() => UIPanelId.LevelComplete;

        /// <summary>
        /// Returns the panel ID to show when level is lost.
        /// </summary>
        protected virtual UIPanelId GetLosePanelId() => UIPanelId.GameOver;

        /// <summary>
        /// Called after a level is won, before UI is shown.
        /// </summary>
        protected virtual void OnLevelWon(LevelEndReason reason) { }

        /// <summary>
        /// Called after a level is lost, before UI is shown.
        /// </summary>
        protected virtual void OnLevelLost(LevelEndReason reason) { }

        /// <summary>
        /// Called when all levels in a world are completed.
        /// </summary>
        protected virtual void OnWorldWasCompleted(int worldIndex) { }

        /// <summary>
        /// Called when a new world is unlocked.
        /// </summary>
        protected virtual void OnWorldWasUnlocked(int worldIndex) { }

        /// <summary>
        /// Whether to automatically show UI panels on win/lose.
        /// Override to return false if you want manual control.
        /// </summary>
        protected virtual bool AutoShowEndPanels => true;

        #endregion

        #region Initialization

        protected override void Initialize()
        {
            LoadProgress();
            BuildWorldCache();

            // Register with ServiceLocator
            ServiceLocator.Instance.Register<ILevelFlowManager>(this);

        }

        private void LoadProgress()
        {
            if (_progressLoaded) return;

            _progressData = SaveSystem.Load<LevelProgressData>(SaveFileName);
            _currentLevelIndex = _progressData.currentLevel;
            _progressLoaded = true;
        }

        private void BuildWorldCache()
        {
            var worlds = GetWorldConfigs();
            if (worlds == null || worlds.Length == 0)
            {
                _cachedWorlds = null;
                _worldStartLevels = null;
                return;
            }

            _cachedWorlds = worlds;
            _worldStartLevels = new int[worlds.Length];

            int startLevel = 1;
            for (int i = 0; i < worlds.Length; i++)
            {
                _worldStartLevels[i] = startLevel;
                startLevel += worlds[i].levelCount;
            }
        }

        #endregion

        #region Level Control

        public void StartLevel(int levelIndex)
        {
            if (levelIndex < 1)
            {
                Debug.LogWarning($"[LevelFlowManager] Invalid level index: {levelIndex}");
                return;
            }

            // Cleanup previous level if any
            CleanupEndPanelSubscription();
            if (_currentState != LevelState.Idle)
            {
                OnLevelCleanupRequested?.Invoke();
            }

            _currentLevelIndex = levelIndex;
            _lastEndReason = LevelEndReason.None;

            SetState(LevelState.Initializing);

            // Notify game to setup the level using actual content index
            int actualLevelIndex = GetActualLevelIndex(levelIndex);
            OnLevelSetupRequested?.Invoke(actualLevelIndex);

            SetState(LevelState.Playing);
            OnLevelStarted?.Invoke(levelIndex); // Fire with progressive index for tutorial system
        }

        public void RestartLevel()
        {
            StartLevel(_currentLevelIndex);
        }

        public void PauseLevel()
        {
            if (_currentState != LevelState.Playing) return;

            SetState(LevelState.Paused);
            OnLevelPaused?.Invoke();
        }

        public void ResumeLevel()
        {
            if (_currentState != LevelState.Paused) return;

            SetState(LevelState.Playing);
            OnLevelResumed?.Invoke();
        }

        public void WinLevel(LevelEndReason reason = LevelEndReason.AllGoalsComplete)
        {
            if (_currentState != LevelState.Playing && _currentState != LevelState.Paused) return;

            _lastEndReason = reason;
            SetState(LevelState.Won);

            // Hook for game-specific logic
            OnLevelWon(reason);

            // Fire while CurrentLevelIndex still points at the completed level.
            OnLevelEnded?.Invoke(reason);

            // Advance progress after end subscribers have observed the completed level.
            UpdateProgressOnWin();

            // Show UI
            if (AutoShowEndPanels)
            {
                ShowEndPanelAsync(GetWinPanelId()).Forget();
            }
        }

        public void LoseLevel(LevelEndReason reason)
        {
            if (_currentState != LevelState.Playing && _currentState != LevelState.Paused) return;

            _lastEndReason = reason;
            SetState(LevelState.Lost);

            // Hook for game-specific logic
            OnLevelLost(reason);

            // Fire events
            OnLevelEnded?.Invoke(reason);

            // Show UI
            if (AutoShowEndPanels)
            {
                ShowEndPanelAsync(GetLosePanelId()).Forget();
            }
        }

        public void QuitLevel()
        {
            if (_currentState == LevelState.Idle) return;

            _lastEndReason = LevelEndReason.PlayerQuit;
            OnLevelEnded?.Invoke(_lastEndReason);

            CleanupEndPanelSubscription();
            OnLevelCleanupRequested?.Invoke();
            SetState(LevelState.Idle);
        }

        #endregion

        #region Progression

        public void AdvanceToNextLevel()
        {
            int nextLevel = _currentLevelIndex + 1;
            // No wrapping - progressive level index keeps incrementing
            StartLevel(nextLevel);
        }

        public void SaveProgress()
        {
            if (_progressData == null) return;

            _progressData.UpdateLastPlayed();
            SaveSystem.Save(_progressData, SaveFileName);
        }

        private void SaveProgressAsync()
        {
            if (_progressData == null) return;

            _progressData.UpdateLastPlayed();
            SaveSystem.SaveAsync(_progressData, SaveFileName).Forget();
        }

        public LevelProgressData GetProgressData()
        {
            return _progressData;
        }

        private void UpdateProgressOnWin()
        {
            int completedLevel = _currentLevelIndex;
            int nextLevel = completedLevel + 1;

            _progressData.totalLevelsCompleted++;

            // Update current level to next (no wrapping - keeps incrementing)
            _progressData.currentLevel = nextLevel;
            _currentLevelIndex = nextLevel;

            // Update highest level reached (capped at total for display purposes)
            int actualNextLevel = GetActualLevelIndex(nextLevel);
            if (actualNextLevel > _progressData.highestLevelReached)
            {
                _progressData.highestLevelReached = actualNextLevel;
            }

            // Check world completion and unlocking
            if (UsesWorldSystem)
            {
                int currentWorld = GetWorldForLevel(completedLevel);
                int lastLevelOfWorld = GetLastLevelOfWorld(currentWorld);

                // Check if we just completed the last level of a world
                if (completedLevel == lastLevelOfWorld)
                {
                    _progressData.totalWorldsCompleted++;
                    _progressData.currentWorld = Mathf.Min(currentWorld + 1, WorldCount);

                    OnWorldWasCompleted(currentWorld);
                    OnWorldCompleted?.Invoke(currentWorld);

                    // Check if next world was unlocked
                    int nextWorld = currentWorld + 1;
                    if (nextWorld <= WorldCount && nextWorld > _progressData.highestWorldReached)
                    {
                        _progressData.highestWorldReached = nextWorld;
                        OnWorldWasUnlocked(nextWorld);
                        OnWorldUnlocked?.Invoke(nextWorld);
                    }
                }
            }

            SaveProgressAsync();
        }

        #endregion

        #region World System Methods

        public int GetLevelIndexInWorld(int globalLevelIndex)
        {
            if (!UsesWorldSystem) return globalLevelIndex;

            int worldIndex = GetWorldForLevel(globalLevelIndex);
            int firstLevel = GetFirstLevelOfWorld(worldIndex);
            return globalLevelIndex - firstLevel + 1;
        }

        public int GetWorldForLevel(int globalLevelIndex)
        {
            if (!UsesWorldSystem || _worldStartLevels == null) return 1;

            for (int i = _worldStartLevels.Length - 1; i >= 0; i--)
            {
                if (globalLevelIndex >= _worldStartLevels[i])
                {
                    return i + 1; // 1-based
                }
            }

            return 1;
        }

        public int GetFirstLevelOfWorld(int worldIndex)
        {
            if (!UsesWorldSystem || _worldStartLevels == null) return 1;

            int index = worldIndex - 1; // Convert to 0-based
            if (index < 0 || index >= _worldStartLevels.Length) return 1;

            return _worldStartLevels[index];
        }

        public int GetLastLevelOfWorld(int worldIndex)
        {
            if (!UsesWorldSystem || _cachedWorlds == null) return TotalLevelCount;

            int index = worldIndex - 1; // Convert to 0-based
            if (index < 0 || index >= _cachedWorlds.Length) return TotalLevelCount;

            return GetFirstLevelOfWorld(worldIndex) + _cachedWorlds[index].levelCount - 1;
        }

        public bool IsWorldUnlocked(int worldIndex)
        {
            if (worldIndex <= 1) return true;
            return worldIndex <= HighestWorldReached;
        }

        public WorldConfig GetWorldConfig(int worldIndex)
        {
            if (!UsesWorldSystem || _cachedWorlds == null) return null;

            int index = worldIndex - 1; // Convert to 0-based
            if (index < 0 || index >= _cachedWorlds.Length) return null;

            return _cachedWorlds[index];
        }

        #endregion

        #region Configuration

        public void SetTotalLevelCount(int count)
        {
            _totalLevelCount = Mathf.Max(1, count);
        }

        #endregion

        #region Private Helpers

        private void SetState(LevelState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        private async UniTaskVoid ShowEndPanelAsync(UIPanelId panelId)
        {
            var uiManager = UIManager.Instance;
            if (uiManager == null) return;

            try
            {
                if (_endPanelDelay > 0f)
                    await Cysharp.Threading.Tasks.UniTask.Delay((int)(_endPanelDelay * 1000));
                var panel = await uiManager.OpenPanelAsync(panelId);
                if (panel != null)
                {
                    _subscribedEndPanel = panel;
                    panel.OnClosed += HandleEndPanelDismissed;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelFlowManager] Failed to show panel {panelId}: {ex.Message}");
            }
        }

        private void HandleEndPanelDismissed(UIPanel panel)
        {
            panel.OnClosed -= HandleEndPanelDismissed;
            _subscribedEndPanel = null;
            OnEndPanelDismissed?.Invoke();
        }

        private void CleanupEndPanelSubscription()
        {
            if (_subscribedEndPanel != null)
            {
                _subscribedEndPanel.OnClosed -= HandleEndPanelDismissed;
                _subscribedEndPanel = null;
            }
        }

        #endregion

        #region Cleanup

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _progressData != null)
            {
                SaveProgress();
            }
        }

        private void OnApplicationQuit()
        {
            if (_progressData != null)
            {
                SaveProgress();
            }
        }

        #endregion
    }
}
