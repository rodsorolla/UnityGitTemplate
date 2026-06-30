using UnityEngine;

namespace Sorolla.Purchasing
{
    [CreateAssetMenu(menuName = "Sorolla/Purchasing/Rewards/Coin", fileName = "Reward_Coins")]
    public class CoinReward : RewardDefinition
    {
        [SerializeField, Min(1)] private int _amount = 1;
        public int Amount => _amount;
        public override GrantPolicy Policy => GrantPolicy.EveryPurchase;
    }
}
