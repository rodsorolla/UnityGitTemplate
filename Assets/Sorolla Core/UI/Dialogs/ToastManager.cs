using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.UI.Dialogs
{
    /// <summary>
    /// Manages toast notifications with queue support.
    /// Ensures toasts are displayed sequentially.
    /// </summary>
    public class ToastManager : MonoBehaviour
    {
        private static ToastManager _instance;

        /// <summary>
        /// Singleton instance. Creates one if it doesn't exist.
        /// </summary>
        public static ToastManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ToastManager");
                    _instance = go.AddComponent<ToastManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private readonly Queue<ToastPanel.Data> _queue = new();
        private bool _isShowing;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// Show a toast message.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="icon">Optional icon sprite</param>
        /// <param name="duration">Optional display duration (uses panel default if null)</param>
        public void ShowToast(string message, Sprite icon = null, float? duration = null)
        {
            _queue.Enqueue(new ToastPanel.Data
            {
                Message = message,
                Icon = icon,
                Duration = duration
            });
            TryShowNext();
        }

        /// <summary>
        /// Show a toast with full configuration.
        /// </summary>
        public void ShowToast(ToastPanel.Data data)
        {
            _queue.Enqueue(data);
            TryShowNext();
        }

        /// <summary>
        /// Clear all pending toasts.
        /// </summary>
        public void ClearQueue()
        {
            _queue.Clear();
        }

        private async void TryShowNext()
        {
            if (_isShowing || _queue.Count == 0) return;
            _isShowing = true;

            var data = _queue.Dequeue();
            var originalCallback = data.OnDismissed;

            // Wrap callback to trigger next toast
            data.OnDismissed = () =>
            {
                originalCallback?.Invoke();
                _isShowing = false;
                TryShowNext();
            };

            var uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                await uiManager.OpenPanelAsync(UIPanelId.Toast, data);
            }
            else
            {
                Debug.LogWarning("ToastManager: UIManager not available. Toast not shown.");
                _isShowing = false;
                TryShowNext();
            }
        }
    }
}
