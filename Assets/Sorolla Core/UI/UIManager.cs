using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ZLinq;
using Sorolla;

// using UnityEngine.AddressableAssets; // Uncomment if using Addressables

namespace Sorolla.UI
{
    public class UIManager : MonoSingleton<UIManager>
    {
        // Events for panel lifecycle - games can subscribe to pause timers, etc.
        public event System.Action<UIPanel> OnPanelOpened;
        public event System.Action<UIPanel> OnPanelClosed;

        [Header("Registry & Parents")]
        [SerializeField] private UIRegistry _registry;

        [SerializeField] private Transform _screensParent;
        [SerializeField] private Transform _panelsParent;
        [SerializeField] private Canvas _mainCanvas;

        private readonly Stack<UIScreen> _screenStack = new();
        private readonly List<UIPanel> _panels = new();

        // Cache of instantiated screens/panels by ID for reuse
        private readonly Dictionary<UIScreenId, UIScreen> _screenCache = new();
        private readonly Dictionary<UIPanelId, List<UIPanel>> _panelCache = new();

        public Canvas MainCanvas => _mainCanvas;
        public Transform PanelsParent => _panelsParent;

        /// <summary>
        /// Initialize the gameplay UI. Resolves IGameplayUI from ServiceLocator.
        /// Game must register their IGameplayUI implementation before calling this.
        /// </summary>
        public void BuildGameUI()
        {
            var gameplayUI = ServiceLocator.Instance.TryResolve<IGameplayUI>();
            if (gameplayUI != null)
            {
                gameplayUI.Init();
            }
            else
            {
                Debug.LogWarning("UIManager: No IGameplayUI registered. Game should register one via ServiceLocator.");
            }
        }

        #region Public API
        
        /// <summary>
        /// Show or hide the gameplay UI. Resolves IGameplayUI from ServiceLocator.
        /// </summary>
        public void ShowGameUI(bool show)
        {
            var gameplayUI = ServiceLocator.Instance.TryResolve<IGameplayUI>();
            gameplayUI?.ShowGameplayUI(show);
        }
        
        // Fire-and-forget wrapper that logs exceptions
        public void HandleBackSafe()
        {
            HandleBackAsync().Forget();
        }

        public async UniTask HandleBackAsync()
        {
            // Panels intercept first by highest BackPriority
            if (_panels.Count > 0)
            {
                var topPanel = _panels.AsValueEnumerable().OrderByDescending(p => p.BackPriority).FirstOrDefault();
                if (topPanel != null && topPanel.HandleBack()) return;

                // Default: close top-most visible panel if it doesn't handle back
                var last = _panels.AsValueEnumerable().LastOrDefault();
                if (last != null)
                {
                    await ClosePanelAsync(last);
                    return;
                }
            }

            // Then Screen
            if (_screenStack.Count > 0)
            {
                var topScreen = _screenStack.Peek();
                if (topScreen != null && topScreen.HandleBack()) return;

                await PopScreenAsync(); // go back
            }
        }

        public UniTask<UIScreen> PushScreenAsync(UIScreenId id, object args = null, bool clearStack = false)
        {
            return PushScreenInternalAsync(id, args, clearStack);
        }

        public async UniTask PopScreenAsync()
        {
            if (_screenStack.Count <= 1)
            {
                // Optional: exit app or show confirm dialog
                return;
            }

            var top = _screenStack.Pop();
            if (top != null) await top.HideAsync();

            var next = _screenStack.Peek();
            if (next != null) await next.ShowAsync(); // re-show
        }

        public async UniTask<UIPanel> OpenPanelAsync(UIPanelId id, object args = null)
        {
            var panel = await GetOrCreatePanelInstanceAsync(id);
            if (panel == null) return null;
            if (!_panels.Contains(panel)) _panels.Add(panel);
            await panel.ShowAsync(args);
            OnPanelOpened?.Invoke(panel);
            return panel;
        }

        /// <summary>
        /// Open a panel with a custom enter transition.
        /// </summary>
        /// <param name="id">The panel ID to open</param>
        /// <param name="args">Optional arguments to pass to the panel</param>
        /// <param name="transition">Custom transition to play before ShowAsync</param>
        /// <returns>The opened panel, or null if not found</returns>
        public async UniTask<UIPanel> OpenPanelAsync(UIPanelId id, object args, IUITransition transition)
        {
            var panel = await GetOrCreatePanelInstanceAsync(id);
            if (panel == null) return null;
            if (!_panels.Contains(panel)) _panels.Add(panel);

            // Play custom transition before showing
            if (transition != null)
            {
                panel.gameObject.SetActive(true);
                await transition.PlayEnterAsync(panel.transform);
            }

            await panel.ShowAsync(args);
            OnPanelOpened?.Invoke(panel);
            return panel;
        }

        public async UniTask ClosePanelAsync(UIPanel panel)
        {
            if (panel == null) return;
            await panel.HideAsync();
            _panels.Remove(panel);
            OnPanelClosed?.Invoke(panel);
        }

        /// <summary>
        /// Close a panel with a custom exit transition.
        /// </summary>
        /// <param name="panel">The panel to close</param>
        /// <param name="transition">Custom transition to play before HideAsync</param>
        public async UniTask ClosePanelAsync(UIPanel panel, IUITransition transition)
        {
            if (panel == null) return;

            // Play custom transition before hiding
            if (transition != null)
            {
                await transition.PlayExitAsync(panel.transform);
            }

            await panel.HideAsync();
            _panels.Remove(panel);
            OnPanelClosed?.Invoke(panel);
        }

        public async UniTask ClosePanelsByIdAsync(UIPanelId id)
        {
            if (_panelCache.TryGetValue(id, out var list))
            {
                // copy to avoid modification during iteration
                foreach (var p in list.AsValueEnumerable().ToList())
                {
                    if (_panels.Contains(p)) await ClosePanelAsync(p);
                }
            }
        }

        public UIScreen GetTopScreen() => _screenStack.Count > 0 ? _screenStack.Peek() : null;
        
        #endregion Public API
        
        #region Internal


        private async UniTask<UIScreen> PushScreenInternalAsync(UIScreenId id, object args, bool clearStack)
        {
            if (clearStack)
            {
                while (_screenStack.Count > 0)
                {
                    var s = _screenStack.Pop();
                    if (s != null) await s.HideAsync();
                }
            }
            else
            {
                // Hide current top if exists
                if (_screenStack.Count > 0)
                {
                    var top = _screenStack.Peek();
                    if (top != null) await top.HideAsync();
                }
            }

            var screen = await GetOrCreateScreenInstanceAsync(id);
            if (screen == null) return null;

            _screenStack.Push(screen);
            await screen.ShowAsync(args);
            return screen;
        }

        private async UniTask<UIScreen> GetOrCreateScreenInstanceAsync(UIScreenId id)
        {
            await UniTask.CompletedTask; // Synchronous but keeps async signature for consistency
            if (_screenCache.TryGetValue(id, out var s) && s != null) return s;

            if (!_registry.TryGetScreen(id, out var entry))
            {
                Debug.LogError($"UIManager: Screen not found in registry: {id}");
                return null;
            }

            GameObject go = null;

            if (entry.prefab != null)
            {
                go = Object.Instantiate(entry.prefab, _screensParent);
            }

            if (go == null)
            {
                Debug.LogError($"UIManager: Could not instantiate screen: {id}");
                return null;
            }

            s = go.GetComponent<UIScreen>();
            if (s == null)
            {
                Debug.LogError($"UIManager: Prefab for {id} has no UIScreen component.");
                Object.Destroy(go);
                return null;
            }

            go.SetActive(false);
            _screenCache[id] = s;
            return s;
        }

        private async UniTask<UIPanel> GetOrCreatePanelInstanceAsync(UIPanelId id)
        {
            await UniTask.CompletedTask; // Synchronous but keeps async signature for consistency
            // Allow multiple instances per Panel ID if needed; here we reuse a single instance per ID by default
            if (_panelCache.TryGetValue(id, out var list))
            {
                var existing = list.AsValueEnumerable().FirstOrDefault(p => p != null && !p.gameObject.activeSelf);
                if (existing != null) return existing;
            }

            if (!_registry.TryGetPanel(id, out var entry))
            {
                Debug.LogError($"UIManager: Panel not found in registry: {id}");
                return null;
            }

            GameObject go = null;

            if (entry.prefab != null)
            {
                go = Object.Instantiate(entry.prefab, _panelsParent);
            }

            if (go == null)
            {
                Debug.LogError($"UIManager: Could not instantiate panel: {id}");
                return null;
            }

            var p = go.GetComponent<UIPanel>();
            if (p == null)
            {
                Debug.LogError($"UIManager: Prefab for {id} has no UIPanel component.");
                Object.Destroy(go);
                return null;
            }

            go.SetActive(false);

            if (!_panelCache.ContainsKey(id)) _panelCache[id] = new List<UIPanel>();
            _panelCache[id].Add(p);
            return p;
        }
        
        #endregion
    }
}