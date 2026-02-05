namespace Sorolla.UI
{
    /// <summary>
    /// Interface for game-specific gameplay UI components.
    /// Allows UIManager to control gameplay UI without knowing concrete implementation.
    /// </summary>
    public interface IGameplayUI
    {
        /// <summary>
        /// Initialize the gameplay UI with current game state.
        /// </summary>
        void Init();
        
        /// <summary>
        /// Show or hide the gameplay UI (HUD, goals, etc).
        /// </summary>
        void ShowGameplayUI(bool show);
    }
}
