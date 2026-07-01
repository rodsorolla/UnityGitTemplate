using System.Collections.Generic;
using Sorolla.UI;
using UnityEngine;

namespace Template
{
    /// <summary>
    /// Template default <see cref="IIconResolver"/>: a serialized (itemType, itemId) → Sprite map
    /// for the sample reward/currency icons. Data-driven UI surfaces (Tournament rewards, etc.)
    /// resolve sprites through this instead of holding their own inline maps. Returns null for
    /// unmapped pairs (per the interface contract). Registered as <see cref="IIconResolver"/> by
    /// <see cref="TemplateGameManager"/> in the Init boot scene.
    /// </summary>
    public class TemplateIconResolver : MonoBehaviour, IIconResolver
    {
        [System.Serializable]
        private struct Entry
        {
            public string itemType;
            public string itemId;
            public Sprite sprite;
        }

        [Tooltip("itemId may be empty when an itemType has a single sprite.")]
        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public Sprite Resolve(string itemType, string itemId)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.itemType != itemType) continue;
                if (string.IsNullOrEmpty(e.itemId) || e.itemId == itemId) return e.sprite;
            }
            return null;
        }
    }
}
