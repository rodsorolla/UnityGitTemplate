using UnityEngine;
using System;
using System.Threading.Tasks;

namespace Sorolla.UI
{
    /// <summary>
    /// Base class for full-screen UI content.
    /// Override ShowAsync/HideAsync for custom behavior, or use transition hooks for animations.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        public event Action<UIScreen> OnOpened;
        public event Action<UIScreen> OnClosed;

        /// <summary>
        /// Called by UIManager when the screen is becoming visible.
        /// Override for custom show behavior, or use PlayEnterTransitionAsync for animations.
        /// </summary>
        public virtual async Task ShowAsync(object args = null)
        {
            gameObject.SetActive(true);
            await PlayEnterTransitionAsync();
            OnOpened?.Invoke(this);
        }

        /// <summary>
        /// Called by UIManager when hiding this screen.
        /// Override for custom hide behavior, or use PlayExitTransitionAsync for animations.
        /// </summary>
        public virtual async Task HideAsync()
        {
            await PlayExitTransitionAsync();
            gameObject.SetActive(false);
            OnClosed?.Invoke(this);
        }

        /// <summary>
        /// Override to add custom enter transition animation.
        /// Called after SetActive(true), before OnOpened event.
        /// </summary>
        protected virtual Task PlayEnterTransitionAsync() => Task.CompletedTask;

        /// <summary>
        /// Override to add custom exit transition animation.
        /// Called before SetActive(false) and OnClosed event.
        /// </summary>
        protected virtual Task PlayExitTransitionAsync() => Task.CompletedTask;

        /// <summary>
        /// Override to handle back button (Android back, top-left back button, etc.).
        /// Return true if handled, false to let UIManager handle it.
        /// </summary>
        public virtual bool HandleBack() => false;
    }

    /// <summary>
    /// Base class for overlay UI panels (modals, dialogs, popups).
    /// Override ShowAsync/HideAsync for custom behavior, or use transition hooks for animations.
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        public event Action<UIPanel> OnOpened;
        public event Action<UIPanel> OnClosed;

        /// <summary>
        /// Called by UIManager when the panel is being shown.
        /// Override for custom show behavior, or use PlayEnterTransitionAsync for animations.
        /// </summary>
        public virtual async Task ShowAsync(object args = null)
        {
            gameObject.SetActive(true);
            await PlayEnterTransitionAsync();
            OnOpened?.Invoke(this);
        }

        /// <summary>
        /// Called by UIManager when the panel is being hidden.
        /// Override for custom hide behavior, or use PlayExitTransitionAsync for animations.
        /// </summary>
        public virtual async Task HideAsync()
        {
            await PlayExitTransitionAsync();
            gameObject.SetActive(false);
            OnClosed?.Invoke(this);
        }

        /// <summary>
        /// Override to add custom enter transition animation.
        /// Called after SetActive(true), before OnOpened event.
        /// </summary>
        protected virtual Task PlayEnterTransitionAsync() => Task.CompletedTask;

        /// <summary>
        /// Override to add custom exit transition animation.
        /// Called before SetActive(false) and OnClosed event.
        /// </summary>
        protected virtual Task PlayExitTransitionAsync() => Task.CompletedTask;

        /// <summary>
        /// Protected helper so derived types can raise the opened event.
        /// </summary>
        protected void RaiseOpened() => OnOpened?.Invoke(this);

        /// <summary>
        /// Protected helper so derived types can raise the closed event.
        /// </summary>
        protected void RaiseClosed() => OnClosed?.Invoke(this);

        /// <summary>
        /// Override to handle back button. Return true if handled.
        /// Panels may intercept back (e.g., modals blocking navigation).
        /// </summary>
        public virtual bool HandleBack() => false;

        /// <summary>
        /// Back button priority. Higher values intercept back first.
        /// Default is 0. Override to change priority.
        /// </summary>
        public virtual int BackPriority => 0;
    }
}