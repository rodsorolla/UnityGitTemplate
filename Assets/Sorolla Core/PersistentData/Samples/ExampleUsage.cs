using UnityEngine;
using Sorolla;

namespace Sorolla.PersistentData.Samples
{
    /// <summary>
    /// Example showing how to access GameDataService from any script.
    /// </summary>
    public class ExampleUsage : MonoBehaviour
    {
        private ExampleGameDataService _gameData;

        private void Start()
        {
            // Get the service from ServiceLocator
            _gameData = ServiceLocator.Instance.Resolve<IGameDataService>() as ExampleGameDataService;

            if (_gameData == null || !_gameData.IsLoaded)
            {
                Debug.LogError("GameDataService not loaded!");
                return;
            }

            // Access data directly
            Debug.Log($"Player has {_gameData.Player.coins} coins");
        }

        // Example: Called when player collects a coin
        public void OnCoinCollected()
        {
            _gameData.Player.coins++;
            // Data auto-saves on app pause/quit, or you can force save:
            // _gameData.SaveAll();
        }

        // Example: Called when player completes a level
        public void OnLevelComplete()
        {
            _gameData.Player.level++;
            _gameData.Player.experience += 100;
        }

        // Example: Settings toggle
        public void ToggleVibration()
        {
            _gameData.Settings.vibrationEnabled = !_gameData.Settings.vibrationEnabled;
        }
    }
}
