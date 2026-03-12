using System.Threading.Tasks;
using Sorolla.UI;
using UnityEngine;

namespace Sorolla.LevelFlow
{
    /// <summary>
    /// Orchestrates the session flow: MainMenu → Gameplay → EndPanel → MainMenu.
    /// Place in the Game scene. Games can extend for custom behavior.
    /// </summary>
    public class LevelSessionController : MonoBehaviour
    {
        private ILevelFlowManager _levelFlow;

        protected ILevelFlowManager LevelFlow => _levelFlow;

        protected virtual async void Start()
        {
            // Wait one frame so all Start() methods (LevelController, etc.) subscribe to events
            await Task.Yield();

            _levelFlow = ServiceLocator.Instance.Resolve<ILevelFlowManager>();
            _levelFlow.OnEndPanelDismissed += HandleEndPanelDismissed;

            await ShowMainMenuAsync();
        }

        private void HandleEndPanelDismissed()
        {
            _ = ShowMainMenuAsync();
        }

        protected virtual async Task ShowMainMenuAsync()
        {
            UIManager.Instance.ShowGameUI(false);
            await UIManager.Instance.PushScreenAsync(GetMainMenuScreenId(), null, true);
            OnMainMenuShown();
        }

        /// <summary>
        /// Call from the main menu's Play button to start the current level.
        /// </summary>
        public virtual void RequestStartLevel()
        {
            _ = StartLevelAsync();
        }

        private async Task StartLevelAsync()
        {
            // Hide any active screen (main menu)
            var topScreen = UIManager.Instance.GetTopScreen();
            if (topScreen != null)
                await topScreen.HideAsync();

            UIManager.Instance.ShowGameUI(true);
            OnLevelStarting(_levelFlow.CurrentLevelIndex);
            _levelFlow.StartLevel(_levelFlow.CurrentLevelIndex);
        }

        protected virtual UIScreenId GetMainMenuScreenId() => UIScreenId.MainMenu;
        protected virtual void OnMainMenuShown() { }
        protected virtual void OnLevelStarting(int levelIndex) { }

        protected virtual void OnDestroy()
        {
            if (_levelFlow != null)
                _levelFlow.OnEndPanelDismissed -= HandleEndPanelDismissed;
        }
    }
}
