using Cysharp.Threading.Tasks;
using Sorolla.UI;

namespace Sorolla.Lives
{
    /// <summary>
    /// Helper for the level-start gate. _Game calls this before LevelFlowManager.StartLevel.
    /// </summary>
    public static class LivesGate
    {
        /// <summary>
        /// True if the player should be shown the Out-of-Lives panel instead of starting this level.
        /// </summary>
        public static bool IsBlockedAt(int progressiveLevelIndex)
        {
            var lives = ServiceLocator.Instance.TryResolve<ILivesService>();
            if (lives == null) return false;
            if (!lives.IsActiveForLevel(progressiveLevelIndex)) return false;
            if (lives.IsBoosterActive) return false;
            return lives.Current <= 0;
        }

        /// <summary>
        /// If the player is blocked, opens UIPanelId.OutOfLives and returns true.
        /// Otherwise returns false. Caller should NOT call StartLevel when this returns true.
        /// </summary>
        public static async UniTask<bool> GuardStartLevelAsync(int progressiveLevelIndex)
        {
            if (!IsBlockedAt(progressiveLevelIndex)) return false;
            var ui = UIManager.Instance;
            if (ui != null) await ui.OpenPanelAsync(UIPanelId.OutOfLives);
            return true;
        }
    }
}
