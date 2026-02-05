namespace Sorolla
{
    /// <summary>
    /// Core service locator interface for dependency injection.
    /// Allows Sorolla Core to remain agnostic of game-specific implementations.
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// Register a service instance.
        /// </summary>
        void Register<T>(T service) where T : class;
        
        /// <summary>
        /// Resolve a service instance.
        /// </summary>
        T Resolve<T>() where T : class;
        
        /// <summary>
        /// Try to resolve a service, returns null if not found.
        /// </summary>
        T TryResolve<T>() where T : class;
        
        /// <summary>
        /// Check if a service is registered.
        /// </summary>
        bool Has<T>() where T : class;
        
        /// <summary>
        /// Clear all registered services.
        /// </summary>
        void Clear();
    }
}

