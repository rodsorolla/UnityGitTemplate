using System.Threading.Tasks;

namespace Sorolla.LevelFlow
{
    /// <summary>
    /// One animation in the post-level-completion sequence (live event progress
    /// bar advance, milestone unlock burst, etc.). Implementations register
    /// themselves with <see cref="IPostLevelAnimationQueue"/>; the level flow
    /// awaits the queue in priority order after the level-complete panel closes
    /// and after the coin reward animation finishes.
    /// </summary>
    public interface IPostLevelAnimation
    {
        /// <summary>
        /// Lower priority runs first. Use stable, well-spaced values (10, 20, 30...)
        /// so new providers can interleave without touching others.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Return false to skip this provider for the current call (e.g. no
        /// pending progress, event inactive, panel hidden). The queue evaluates
        /// this lazily at PlayAllAsync time, never caches.
        /// </summary>
        bool ShouldPlay { get; }

        /// <summary>
        /// Run the animation. The queue awaits this before moving on to the
        /// next provider. Implementations must complete the returned task even
        /// on early-return so the sequence doesn't stall.
        /// </summary>
        Task PlayAsync();
    }
}
