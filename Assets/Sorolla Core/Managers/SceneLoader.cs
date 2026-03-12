using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sorolla
{
    /// <summary>
    /// Static utility wrapping Unity's scene loading with async/await and progress callbacks.
    /// No loading screen UI — that's game-specific.
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>
        /// Loads a scene asynchronously (single mode — replaces current scene).
        /// </summary>
        /// <param name="sceneName">Scene name (must be in Build Settings)</param>
        /// <param name="onProgress">Optional progress callback (0-1)</param>
        public static async Task LoadSceneAsync(string sceneName, Action<float> onProgress = null)
        {
            await LoadSceneInternalAsync(sceneName, LoadSceneMode.Single, onProgress);
        }

        /// <summary>
        /// Loads a scene additively (keeps current scene).
        /// </summary>
        /// <param name="sceneName">Scene name (must be in Build Settings)</param>
        /// <param name="onProgress">Optional progress callback (0-1)</param>
        public static async Task LoadSceneAdditiveAsync(string sceneName, Action<float> onProgress = null)
        {
            await LoadSceneInternalAsync(sceneName, LoadSceneMode.Additive, onProgress);
        }

        /// <summary>
        /// Unloads a scene asynchronously.
        /// </summary>
        /// <param name="sceneName">Scene name to unload</param>
        public static async Task UnloadSceneAsync(string sceneName)
        {
            var op = SceneManager.UnloadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] Failed to unload scene '{sceneName}'.");
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            op.completed += _ => tcs.TrySetResult(true);
            await tcs.Task;
        }

        /// <summary>
        /// Reloads the currently active scene.
        /// </summary>
        /// <param name="onProgress">Optional progress callback (0-1)</param>
        public static async Task ReloadCurrentSceneAsync(Action<float> onProgress = null)
        {
            var currentScene = SceneManager.GetActiveScene().name;
            await LoadSceneAsync(currentScene, onProgress);
        }

        private static async Task LoadSceneInternalAsync(string sceneName, LoadSceneMode mode, Action<float> onProgress)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] Failed to start loading scene '{sceneName}'. Is it in Build Settings?");
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            op.completed += _ => tcs.TrySetResult(true);

            if (onProgress != null)
            {
                while (!op.isDone)
                {
                    onProgress.Invoke(op.progress);
                    await Task.Yield();
                }
                onProgress.Invoke(1f);
            }

            await tcs.Task;
        }
    }
}
