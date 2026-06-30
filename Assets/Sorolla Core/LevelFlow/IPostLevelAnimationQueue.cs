using System.Threading.Tasks;

namespace Sorolla.LevelFlow
{
    /// <summary>
    /// Sequencer for post-level-completion animations. Providers register on
    /// enable, unregister on disable; the level flow calls
    /// <see cref="PlayAllAsync"/> to run them in priority order after the coin
    /// reward animation. Resolve via
    /// <c>ServiceLocator.Instance.TryResolve&lt;IPostLevelAnimationQueue&gt;()</c>.
    /// </summary>
    public interface IPostLevelAnimationQueue
    {
        void Register(IPostLevelAnimation animation);
        void Unregister(IPostLevelAnimation animation);

        /// <summary>
        /// Iterates registered providers in ascending Priority order. Skips any
        /// whose <see cref="IPostLevelAnimation.ShouldPlay"/> is false. Awaits
        /// each <see cref="IPostLevelAnimation.PlayAsync"/> sequentially.
        /// </summary>
        /// <param name="interAnimationDelaySeconds">Delay inserted between
        /// successive animations. 0 = back-to-back.</param>
        Task PlayAllAsync(float interAnimationDelaySeconds = 0f);
    }
}
