using System;

namespace Sorolla.PowerUps
{
    /// <summary>
    /// Event args for power-up quantity changes.
    /// </summary>
    public readonly struct PowerUpQuantityChangedEventArgs
    {
        public PowerUpId PowerUpId { get; }
        public int PreviousQuantity { get; }
        public int NewQuantity { get; }

        public PowerUpQuantityChangedEventArgs(PowerUpId powerUpId, int previousQuantity, int newQuantity)
        {
            PowerUpId = powerUpId;
            PreviousQuantity = previousQuantity;
            NewQuantity = newQuantity;
        }
    }

    /// <summary>
    /// Event args for power-up unlock events.
    /// </summary>
    public readonly struct PowerUpUnlockedEventArgs
    {
        public PowerUpId PowerUpId { get; }
        public PowerUpDefinitionBase Definition { get; }
        public int UnlockedAtLevel { get; }

        public PowerUpUnlockedEventArgs(PowerUpId powerUpId, PowerUpDefinitionBase definition, int unlockedAtLevel)
        {
            PowerUpId = powerUpId;
            Definition = definition;
            UnlockedAtLevel = unlockedAtLevel;
        }
    }

    /// <summary>
    /// Service interface for managing power-up inventory and unlocks.
    /// </summary>
    public interface IPowerUpService
    {
        /// <summary>
        /// Gets the current quantity of a power-up.
        /// </summary>
        int GetQuantity(PowerUpId powerUpId);

        /// <summary>
        /// Checks if a power-up is unlocked (either always unlocked or player has reached unlock level).
        /// </summary>
        bool IsUnlocked(PowerUpId powerUpId);

        /// <summary>
        /// Checks if a power-up can be used (unlocked and quantity > 0, or first use is free).
        /// </summary>
        bool CanUse(PowerUpId powerUpId);

        /// <summary>
        /// Checks if the first use of this power-up is free (hasn't been used yet).
        /// </summary>
        bool IsFirstUseFree(PowerUpId powerUpId);

        /// <summary>
        /// Attempts to use a power-up, decrementing quantity if successful.
        /// </summary>
        /// <returns>True if successful, false if not unlocked or quantity is 0.</returns>
        bool TryUse(PowerUpId powerUpId);

        /// <summary>
        /// Adds quantity to a power-up (respects max quantity).
        /// </summary>
        void AddQuantity(PowerUpId powerUpId, int amount);

        /// <summary>
        /// Attempts to purchase a power-up using the configured currency cost.
        /// </summary>
        /// <returns>True if purchase was successful.</returns>
        bool TryPurchase(PowerUpId powerUpId);

        /// <summary>
        /// Checks if the player can afford to purchase a power-up.
        /// </summary>
        bool CanAffordPurchase(PowerUpId powerUpId);

        /// <summary>
        /// Gets the definition for a power-up by ID.
        /// </summary>
        PowerUpDefinitionBase GetDefinition(PowerUpId powerUpId);

        /// <summary>
        /// Checks if the unlock notification has been shown for a power-up.
        /// </summary>
        bool HasSeenUnlockNotification(PowerUpId powerUpId);

        /// <summary>
        /// Marks the unlock notification as shown.
        /// </summary>
        void MarkUnlockNotificationSeen(PowerUpId powerUpId);

        /// <summary>
        /// Event fired when a power-up quantity changes.
        /// </summary>
        event Action<PowerUpQuantityChangedEventArgs> OnQuantityChanged;

        /// <summary>
        /// Event fired when a power-up is unlocked.
        /// Subscribe to show unlock notifications/tutorials.
        /// </summary>
        event Action<PowerUpUnlockedEventArgs> OnPowerUpUnlocked;

        /// <summary>
        /// Event fired when a power-up is used.
        /// </summary>
        event Action<PowerUpId> OnPowerUpUsed;

        /// <summary>
        /// Manually saves power-up data to disk.
        /// Called automatically on pause/quit.
        /// </summary>
        void Save();

        /// <summary>
        /// Manually loads power-up data from disk.
        /// Called automatically on startup.
        /// </summary>
        void Load();

        /// <summary>
        /// Manually checks for newly unlocked power-ups based on current level.
        /// Called automatically when level is won.
        /// </summary>
        void CheckUnlocks(int currentHighestLevel);
    }
}
