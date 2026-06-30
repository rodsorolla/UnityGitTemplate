using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.Cosmetics
{
    /// <summary>
    /// Populates the skin screen: instantiates one card per catalog entry into the
    /// container and refreshes them when the service reports a change.
    /// </summary>
    public class SkinScreenController : MonoBehaviour
    {
        [SerializeField] private SkinCatalog _catalog;
        [SerializeField] private SkinCardView _cardPrefab;
        [SerializeField] private Transform _cardContainer;

        private readonly List<SkinCardView> _cards = new List<SkinCardView>();
        private ISkinService _service;

        private void Start()
        {
            _service = ServiceLocator.Instance.TryResolve<ISkinService>();
            if (_service == null)
            {
                Debug.LogError("[SkinScreenController] ISkinService not found.");
                return;
            }

            BuildCards();
            _service.OnChanged += RefreshAll;
        }

        private void OnDestroy()
        {
            if (_service != null) _service.OnChanged -= RefreshAll;
        }

        private void BuildCards()
        {
            foreach (var definition in _catalog.Skins)
            {
                var card = Instantiate(_cardPrefab, _cardContainer);
                card.Initialize(definition, _service);
                _cards.Add(card);
            }
        }

        private void RefreshAll()
        {
            foreach (var card in _cards) card.Refresh();
        }
    }
}
