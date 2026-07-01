using System.Text;
using Cysharp.Threading.Tasks;
using Sorolla.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Purchasing
{
    /// <summary>
    /// Shown after a successful first-time purchase. Displays the product's reward bundle.
    /// Agnostic: summarizes only the two Core reward types (coins, entitlements); unknown
    /// reward types fall back to their type name.
    /// Opened via UIManager.OpenPanelAsync(UIPanelId.PurchaseCompleted, new PurchaseCompletedData(product)).
    /// </summary>
    public class PurchaseCompletedPanel : UIPanel
    {
        [Header("Purchase UI")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _rewardsText;
        [SerializeField] private Image _icon;

        public struct PurchaseCompletedData
        {
            public ProductDefinition Product;
            public PurchaseCompletedData(ProductDefinition product) { Product = product; }
        }

        private ProductDefinition _product;

        public override async UniTask ShowAsync(object args = null)
        {
            _product = args is PurchaseCompletedData data ? data.Product : args as ProductDefinition;
            UpdateUI();
            await base.ShowAsync(args);
            ServiceLocator.Instance?.TryResolve<IHapticsService>()?.PlayImpact(HapticsIntensity.Medium);
        }

        private void UpdateUI()
        {
            if (_product == null)
            {
                if (_titleText != null) _titleText.text = "Purchase complete";
                if (_rewardsText != null) _rewardsText.text = string.Empty;
                if (_icon != null) _icon.enabled = false;
                return;
            }

            if (_titleText != null)
                _titleText.text = string.IsNullOrEmpty(_product.DisplayTitleKey)
                    ? "Purchase complete"
                    : _product.DisplayTitleKey;

            if (_icon != null)
            {
                _icon.enabled = _product.Icon != null;
                if (_product.Icon != null) _icon.sprite = _product.Icon;
            }

            if (_rewardsText != null)
                _rewardsText.text = BuildRewardSummary(_product);
        }

        public static string BuildRewardSummary(ProductDefinition product)
        {
            var sb = new StringBuilder();
            foreach (var r in product.Rewards)
            {
                if (r == null) continue;
                switch (r)
                {
                    case CoinReward c:
                        sb.AppendLine($"+{c.Amount:N0} coins");
                        break;
                    case EntitlementReward e:
                        sb.AppendLine(e.Key == EntitlementService.NoAdsKey ? "No Ads unlocked" : $"{e.Key} unlocked");
                        break;
                    default:
                        sb.AppendLine(r.GetType().Name);
                        break;
                }
            }
            return sb.ToString().TrimEnd();
        }
    }
}
