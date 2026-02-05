using UnityEngine;

namespace Sorolla.LevelFlow
{
    [System.Serializable]
    public struct LevelObjectEntry
    {
        [Tooltip("Object to hide/show")]
        public GameObject Object;

        [Tooltip("Reveal this object when level is greater or equal to this value (1-based)")]
        public int RevealLevel;
    }

    /// <summary>
    /// Shows/hides GameObjects based on current level index.
    /// Objects are hidden by default and revealed when player reaches the specified level.
    /// </summary>
    public class LevelObjectsHider : MonoBehaviour
    {
        [Header("Objects to Hide/Show")]
        [SerializeField] private LevelObjectEntry[] _entries;

        private ILevelFlowManager _levelFlowManager;
        private bool _subscribed;

        private void Awake()
        {
            // Hide all objects immediately on Awake, before anything else runs
            HideAll();
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void HideAll()
        {
            if (_entries == null) return;

            foreach (var entry in _entries)
            {
                if (entry.Object == null) continue;
                entry.Object.SetActive(false);
            }
        }

        private void Initialize()
        {
            _levelFlowManager = ServiceLocator.Instance.TryResolve<ILevelFlowManager>();
            if (_levelFlowManager == null) return;

            ApplyLevel(_levelFlowManager.CurrentLevelIndex);

            if (!_subscribed)
            {
                _levelFlowManager.OnLevelStarted += HandleLevelStarted;
                _subscribed = true;
            }
        }

        private void OnDisable()
        {
            if (_levelFlowManager != null && _subscribed)
            {
                _levelFlowManager.OnLevelStarted -= HandleLevelStarted;
                _subscribed = false;
            }
        }

        private void HandleLevelStarted(int levelIndex)
        {
            ApplyLevel(levelIndex);
        }

        private void ApplyLevel(int levelIndex)
        {
            if (_entries == null) return;

            foreach (var entry in _entries)
            {
                if (entry.Object == null) continue;
                bool shouldShow = levelIndex >= entry.RevealLevel;
                entry.Object.SetActive(shouldShow);
            }
        }
    }
}
