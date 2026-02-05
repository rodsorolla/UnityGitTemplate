using Sorolla.LevelFlow;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sorolla.DevTools
{
    /// <summary>
    /// Debug helper: Press R to restart, C to complete the current level.
    /// </summary>
    public class DebugLevelHelper : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private Key _resetKey = Key.R;
        [SerializeField] private Key _completeKey = Key.C;

        private ILevelFlowManager _levelFlow;

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current[_resetKey].wasPressedThisFrame)
            {
                Debug.Log($"[DebugLevelHelper] {_resetKey} key pressed!");
                RestartLevel();
            }

            if (Keyboard.current[_completeKey].wasPressedThisFrame)
            {
                Debug.Log($"[DebugLevelHelper] {_completeKey} key pressed!");
                CompleteLevel();
            }
        }

        private ILevelFlowManager GetLevelFlow()
        {
            _levelFlow ??= ServiceLocator.Instance.Resolve<ILevelFlowManager>();
            return _levelFlow;
        }

        private void RestartLevel()
        {
            var lf = GetLevelFlow();
            if (lf != null)
            {
                Debug.Log("[DebugLevelHelper] Restarting level...");
                lf.RestartLevel();
            }
            else
            {
                Debug.LogWarning("[DebugLevelHelper] ILevelFlowManager not found");
            }
        }

        private void CompleteLevel()
        {
            var lf = GetLevelFlow();
            if (lf != null)
            {
                Debug.Log("[DebugLevelHelper] Completing level...");
                lf.WinLevel();
            }
            else
            {
                Debug.LogWarning("[DebugLevelHelper] ILevelFlowManager not found");
            }
        }
#endif
    }
}
