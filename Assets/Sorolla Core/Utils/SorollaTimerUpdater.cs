using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// MonoBehaviour that drives <see cref="SorollaTimer"/> updates.
    /// Auto-created on first timer use. Persists across scenes.
    /// </summary>
    public class SorollaTimerUpdater : MonoBehaviour
    {
        private static SorollaTimerUpdater _instance;

        /// <summary>
        /// Ensures the updater exists. Called automatically by SorollaTimer.
        /// </summary>
        public static void EnsureExists()
        {
            if (_instance != null) return;

            var go = new GameObject("[SorollaTimerUpdater]");
            _instance = go.AddComponent<SorollaTimerUpdater>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            SorollaTimer.UpdateAll(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
