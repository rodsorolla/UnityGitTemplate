namespace Sorolla.PowerUps
{
    /// <summary>
    /// Power-up identifiers.
    /// </summary>
    public enum PowerUpId
    {
        Undo = 0,
        Shuffle = 1,
        FreezeTimer = 2,
        AutoMatch = 3
    }

    /// <summary>
    /// Extension methods for PowerUpId enum.
    /// </summary>
    public static class PowerUpIdExtensions
    {
        /// <summary>
        /// Converts enum to string key for persistence.
        /// </summary>
        public static string ToKey(this PowerUpId id)
        {
            return id switch
            {
                PowerUpId.Undo => "undo",
                PowerUpId.Shuffle => "shuffle",
                PowerUpId.FreezeTimer => "freeze_timer",
                PowerUpId.AutoMatch => "auto_match",
                _ => id.ToString().ToLowerInvariant()
            };
        }
    }
}
