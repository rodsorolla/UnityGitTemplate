using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.Purchasing
{
    [CreateAssetMenu(menuName = "Sorolla/Purchasing/Catalog", fileName = "PurchasingCatalog")]
    public class PurchasingCatalog : ScriptableObject
    {
        [SerializeField] private List<ProductDefinition> _products = new();
        public IReadOnlyList<ProductDefinition> Products => _products;

        public ProductDefinition Find(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            for (int i = 0; i < _products.Count; i++)
            {
                if (_products[i] != null && _products[i].ProductId == productId)
                    return _products[i];
            }
            return null;
        }
    }
}
