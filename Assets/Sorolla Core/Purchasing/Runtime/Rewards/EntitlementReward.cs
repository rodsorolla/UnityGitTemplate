using UnityEngine;

namespace Sorolla.Purchasing
{
    [CreateAssetMenu(menuName = "Sorolla/Purchasing/Rewards/Entitlement", fileName = "Reward_Entitlement")]
    public class EntitlementReward : RewardDefinition
    {
        [SerializeField] private string _entitlementKey;
        public string Key => _entitlementKey;
        public override GrantPolicy Policy => GrantPolicy.OncePerProduct;
    }
}
