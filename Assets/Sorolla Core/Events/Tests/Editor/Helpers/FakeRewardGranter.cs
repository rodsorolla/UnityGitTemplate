using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Sorolla.Events.Tests.Helpers
{
    public sealed class FakeRewardGranter : IRewardGranter
    {
        public readonly List<(EventReward reward, RewardGrantContext ctx)> Grants =
            new List<(EventReward, RewardGrantContext)>();

        public bool NextGrantReturns = true;

        public UniTask<bool> Grant(EventReward reward, RewardGrantContext ctx)
        {
            Grants.Add((reward, ctx));
            return UniTask.FromResult(NextGrantReturns);
        }
    }
}
