using Cysharp.Threading.Tasks;

namespace Sorolla.Events
{
    /// <summary>
    /// Game-side adapter: turns abstract <see cref="EventReward"/> into actual
    /// currency/booster/skin grants via existing services (Inventory, etc.).
    /// </summary>
    public interface IRewardGranter
    {
        /// <summary>
        /// Grant a single reward. Implementations should be idempotent and
        /// must not call back into EventManager. Returns true on successful
        /// delivery, false on a failure that should be logged but not retried.
        /// </summary>
        UniTask<bool> Grant(EventReward reward, RewardGrantContext ctx);
    }
}
