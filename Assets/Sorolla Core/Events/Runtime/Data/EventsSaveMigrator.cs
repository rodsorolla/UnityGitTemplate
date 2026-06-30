namespace Sorolla.Events
{
    /// <summary>
    /// Forward-compat hook. v1 is a no-op; future versions branch on
    /// <see cref="EventsSaveData.Version"/> here to rewrite shapes.
    /// </summary>
    public static class EventsSaveMigrator
    {
        /// <summary>
        /// Returns either the input data (when up-to-date) or a migrated copy.
        /// At v1, the input is returned unchanged.
        /// </summary>
        public static EventsSaveData Migrate(EventsSaveData input)
        {
            if (input == null) return new EventsSaveData();
            // No migrations defined yet. When EventsSaveData.CurrentVersion increments,
            // add a switch on input.Version here.
            return input;
        }
    }
}
