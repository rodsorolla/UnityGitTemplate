using System;

namespace Sorolla.LevelFlow
{
    /// <summary>
    /// Interface for level flow management.
    /// Handles level states, progression, and optional world grouping.
    /// Register implementations via ServiceLocator.
    /// </summary>
    public interface ILevelFlowManager
    {
        #region State

        /// <summary>Current state of level gameplay.</summary>
        LevelState CurrentState { get; }

        /// <summary>Reason for the last level end (win or lose).</summary>
        LevelEndReason LastEndReason { get; }

        /// <summary>Whether a level is currently active (Playing or Paused).</summary>
        bool IsLevelActive { get; }

        #endregion

        #region Level Progress

        /// <summary>Current level index (1-based).</summary>
        int CurrentLevelIndex { get; }

        /// <summary>Highest level the player has reached.</summary>
        int HighestLevelReached { get; }

        /// <summary>Total number of levels in the game.</summary>
        int TotalLevelCount { get; }

        /// <summary>
        /// Converts a progressive level index to the actual level content index (1-based).
        /// Example: With 3 levels, progressive level 4 returns actual level 1.
        /// </summary>
        int GetActualLevelIndex(int progressiveLevelIndex);

        #endregion

        #region World System

        /// <summary>Whether this game uses the world/chapter system.</summary>
        bool UsesWorldSystem { get; }

        /// <summary>Current world index (1-based). Returns 1 if not using worlds.</summary>
        int CurrentWorldIndex { get; }

        /// <summary>Highest world the player has unlocked. Returns 1 if not using worlds.</summary>
        int HighestWorldReached { get; }

        /// <summary>Total number of worlds. Returns 1 if not using worlds.</summary>
        int WorldCount { get; }

        /// <summary>
        /// Gets the local level index within a world (1-based).
        /// Example: Level 25 in a 20-levels-per-world setup returns 5 (level 5 of world 2).
        /// Returns the global index if not using worlds.
        /// </summary>
        int GetLevelIndexInWorld(int globalLevelIndex);

        /// <summary>
        /// Gets which world contains the specified level (1-based).
        /// Returns 1 if not using worlds.
        /// </summary>
        int GetWorldForLevel(int globalLevelIndex);

        /// <summary>
        /// Gets the first level index of a world (1-based).
        /// Example: World 2 with 20 levels per world returns 21.
        /// Returns 1 if not using worlds.
        /// </summary>
        int GetFirstLevelOfWorld(int worldIndex);

        /// <summary>
        /// Gets the last level index of a world (1-based).
        /// Example: World 1 with 20 levels returns 20.
        /// </summary>
        int GetLastLevelOfWorld(int worldIndex);

        /// <summary>
        /// Checks if a world is unlocked (player has reached it).
        /// Always returns true for world 1.
        /// </summary>
        bool IsWorldUnlocked(int worldIndex);

        /// <summary>
        /// Gets the WorldConfig for a specific world index.
        /// Returns null if not using worlds or index is out of range.
        /// </summary>
        WorldConfig GetWorldConfig(int worldIndex);

        #endregion

        #region Events - Level

        /// <summary>Fired when level state changes.</summary>
        event Action<LevelState> OnStateChanged;

        /// <summary>Fired when a level needs to be set up. Parameter is the actual level index (after modulo).</summary>
        event Action<int> OnLevelSetupRequested;

        /// <summary>Fired when a level needs to be cleaned up (before new level or on quit).</summary>
        event Action OnLevelCleanupRequested;

        /// <summary>Fired when a level starts (after setup, when Playing begins). Parameter is the progressive level index.</summary>
        event Action<int> OnLevelStarted;

        /// <summary>Fired when a level ends (win or lose).</summary>
        event Action<LevelEndReason> OnLevelEnded;

        /// <summary>Fired when gameplay is paused.</summary>
        event Action OnLevelPaused;

        /// <summary>Fired when gameplay resumes from pause.</summary>
        event Action OnLevelResumed;

        /// <summary>Fired when the end-game panel (win/lose) is dismissed by the player.</summary>
        event Action OnEndPanelDismissed;

        #endregion

        #region Events - World

        /// <summary>Fired when all levels in a world are completed.</summary>
        event Action<int> OnWorldCompleted;

        /// <summary>Fired when a new world is unlocked.</summary>
        event Action<int> OnWorldUnlocked;

        #endregion

        #region Control

        /// <summary>
        /// Starts a specific level.
        /// </summary>
        /// <param name="levelIndex">Level index (1-based)</param>
        void StartLevel(int levelIndex);

        /// <summary>
        /// Restarts the current level.
        /// </summary>
        void RestartLevel();

        /// <summary>
        /// Pauses the current level.
        /// </summary>
        void PauseLevel();

        /// <summary>
        /// Resumes the current level from pause.
        /// </summary>
        void ResumeLevel();

        /// <summary>
        /// Marks the current level as won.
        /// </summary>
        /// <param name="reason">Win reason (defaults to AllGoalsComplete)</param>
        void WinLevel(LevelEndReason reason = LevelEndReason.AllGoalsComplete);

        /// <summary>
        /// Marks the current level as lost.
        /// </summary>
        /// <param name="reason">Lose reason</param>
        void LoseLevel(LevelEndReason reason);

        /// <summary>
        /// Quits the current level (returns to Idle state).
        /// </summary>
        void QuitLevel();

        #endregion

        #region Progression

        /// <summary>
        /// Advances to the next level and starts it.
        /// </summary>
        void AdvanceToNextLevel();

        /// <summary>
        /// Manually saves progress (normally called automatically on win).
        /// </summary>
        void SaveProgress();

        /// <summary>
        /// Gets the current progress data (read-only access).
        /// </summary>
        LevelProgressData GetProgressData();

        #endregion

        #region Configuration

        /// <summary>
        /// Sets the total number of levels. Call this from game code to configure level count.
        /// </summary>
        void SetTotalLevelCount(int count);

        #endregion
    }
}
