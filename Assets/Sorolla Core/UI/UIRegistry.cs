using UnityEngine;
using System.Collections.Generic;

namespace Sorolla.UI
{
    [CreateAssetMenu(fileName = "UIRegistry", menuName = "Sorolla/UI/UIRegistry")]
    public class UIRegistry : ScriptableObject
    {
        [System.Serializable]
        public class ScreenEntry
        {
            public UIScreenId id;
            public GameObject prefab; // Direct reference OR leave null if using AddressablesKey
            public string addressablesKey; // Optional: for async loading
        }

        [System.Serializable]
        public class PanelEntry
        {
            public UIPanelId id;
            public GameObject prefab;
            public string addressablesKey;
        }

        public List<ScreenEntry> screens = new List<ScreenEntry>();
        public List<PanelEntry> panels = new List<PanelEntry>();

        private Dictionary<UIScreenId, ScreenEntry> _screenMap;
        private Dictionary<UIPanelId, PanelEntry> _panelMap;

        void OnEnable()
        {
            RebuildMaps();
        }

        private void RebuildMaps()
        {
            _screenMap = new Dictionary<UIScreenId, ScreenEntry>();
            foreach (var s in screens)
            {
                if (s != null && s.id != UIScreenId.None)
                    _screenMap[s.id] = s;
            }

            _panelMap = new Dictionary<UIPanelId, PanelEntry>();
            foreach (var p in panels)
            {
                if (p != null && p.id != UIPanelId.None)
                    _panelMap[p.id] = p;
            }
        }

        private void EnsureMapsInitialized()
        {
            if (_screenMap == null || _panelMap == null)
            {
                RebuildMaps();
            }
        }

        public bool TryGetScreen(UIScreenId id, out ScreenEntry entry)
        {
            EnsureMapsInitialized();
            entry = null;
            return _screenMap != null && _screenMap.TryGetValue(id, out entry);
        }

        public bool TryGetPanel(UIPanelId id, out PanelEntry entry)
        {
            EnsureMapsInitialized();
            entry = null;
            return _panelMap != null && _panelMap.TryGetValue(id, out entry);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Rebuild maps when edited in inspector
            RebuildMaps();
        }
#endif
    }
}
