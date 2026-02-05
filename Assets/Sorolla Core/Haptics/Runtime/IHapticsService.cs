namespace Sorolla
{
    /// <summary>
    /// Service interface for cross-platform haptic feedback.
    /// </summary>
    public interface IHapticsService
    {
        /// <summary>
        /// Whether haptics are enabled. Persists across sessions.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Whether haptic feedback is supported on the current device.
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Plays an impact haptic with the specified intensity.
        /// </summary>
        void PlayImpact(HapticsIntensity intensity);

        /// <summary>
        /// Plays a light selection haptic (UI feedback).
        /// </summary>
        void PlaySelection();

        /// <summary>
        /// Plays a notification haptic of the specified type.
        /// </summary>
        void PlayNotification(HapticsType type);
    }
}
