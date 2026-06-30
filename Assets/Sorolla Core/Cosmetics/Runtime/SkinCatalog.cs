using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.Cosmetics
{
    /// <summary>Ordered registry of skins. Holds subtype assets polymorphically.</summary>
    [CreateAssetMenu(fileName = "SkinCatalog", menuName = "Sorolla/Cosmetics/Skin Catalog")]
    public class SkinCatalog : ScriptableObject
    {
        [SerializeField] private List<SkinDefinition> _skins = new List<SkinDefinition>();
        public IReadOnlyList<SkinDefinition> Skins => _skins;
    }
}
