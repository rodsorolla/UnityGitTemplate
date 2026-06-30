using System;

namespace Sorolla
{
    /// <summary>
    /// Abstraction over the wall-clock so deterministic tests can advance time.
    /// Resolve via ServiceLocator if registered; otherwise default to SystemTimeSource.Instance.
    /// </summary>
    public interface ITimeSource
    {
        DateTime UtcNow { get; }
    }

    /// <summary>
    /// Default ITimeSource backed by System.DateTime.UtcNow.
    /// </summary>
    public sealed class SystemTimeSource : ITimeSource
    {
        public static readonly SystemTimeSource Instance = new SystemTimeSource();
        private SystemTimeSource() { }
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
