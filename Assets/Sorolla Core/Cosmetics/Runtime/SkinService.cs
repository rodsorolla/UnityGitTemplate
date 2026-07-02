using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.Cosmetics
{
    /// <summary>
    /// Plain C# service tracking skin ownership and selection. Persistence is
    /// injected as load/save delegates so Sorolla.Core stays decoupled from any
    /// game-specific persistence type. Default-unlocked skins are always owned.
    /// </summary>
    public class SkinService : ISkinService
    {
        public const string UnlockedSkinsKey = "unlocked_skins";
        public const string SelectedSkinKey = "selected_skin";

        private readonly List<string> _orderedIds;      // catalog order, for default selection
        private readonly HashSet<string> _allIds;
        private readonly HashSet<string> _defaultUnlocked;
        private readonly HashSet<string> _owned = new HashSet<string>();
        private readonly Func<string, string, string> _load;
        private readonly Action<string, string> _save;

        private string _selectedSkinId;

        public event Action OnChanged;

        public SkinService(
            IEnumerable<string> allSkinIds,
            IEnumerable<string> defaultUnlockedIds,
            Func<string, string, string> loadString,
            Action<string, string> saveString)
        {
            _orderedIds = new List<string>(allSkinIds);
            _allIds = new HashSet<string>(_orderedIds);
            _defaultUnlocked = new HashSet<string>(defaultUnlockedIds);
            _load = loadString;
            _save = saveString;

            LoadOwned();
            LoadSelected();
        }

        public string SelectedSkinId => _selectedSkinId;

        public bool IsUnlocked(string id)
            => _defaultUnlocked.Contains(id) || _owned.Contains(id);

        public void Unlock(string id)
        {
            if (!_allIds.Contains(id)) return;
            if (!_owned.Add(id)) return; // already owned
            SaveOwned();
            OnChanged?.Invoke();
        }

        public void Relock(string id)
        {
            if (_defaultUnlocked.Contains(id)) return; // defaults are always owned
            if (!_owned.Remove(id)) return;            // wasn't owned
            SaveOwned();
            OnChanged?.Invoke();
        }

        public bool Select(string id)
        {
            if (!IsUnlocked(id)) return false;
            if (_selectedSkinId == id) return true;
            _selectedSkinId = id;
            _save(SelectedSkinKey, id);
            OnChanged?.Invoke();
            return true;
        }

        private void LoadOwned()
        {
            _owned.Clear();
            string json = _load(UnlockedSkinsKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var wrapper = JsonUtility.FromJson<StringListWrapper>(json);
                if (wrapper?.ids != null)
                    foreach (var id in wrapper.ids)
                        if (!string.IsNullOrEmpty(id)) _owned.Add(id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SkinService] Failed to load owned skins: {e.Message}");
            }
        }

        private void SaveOwned()
        {
            var wrapper = new StringListWrapper { ids = new List<string>(_owned) };
            _save(UnlockedSkinsKey, JsonUtility.ToJson(wrapper));
        }

        private void LoadSelected()
        {
            string saved = _load(SelectedSkinKey, "");
            if (!string.IsNullOrEmpty(saved) && IsUnlocked(saved))
            {
                _selectedSkinId = saved;
                return;
            }
            _selectedSkinId = FirstDefaultOrNull();
            if (_selectedSkinId != null) _save(SelectedSkinKey, _selectedSkinId);
        }

        private string FirstDefaultOrNull()
        {
            foreach (var id in _orderedIds)
                if (_defaultUnlocked.Contains(id)) return id;
            return null;
        }

        [Serializable]
        private class StringListWrapper { public List<string> ids; }
    }
}
