using System;
using System.Collections.Generic;
using Sorolla.PersistentData;

namespace Sorolla.PowerUps
{
    /// <summary>
    /// Persistent data for power-up states (quantities and unlocks).
    /// </summary>
    [Serializable]
    public class PowerUpData : ISaveData
    {
        public int Version => 1;

        /// <summary>
        /// State for a single power-up.
        /// </summary>
        [Serializable]
        public class PowerUpState
        {
            public int quantity;
            public bool isUnlocked;
            public bool hasSeenUnlockNotification;
            public bool hasUsedFirstFree;

            public PowerUpState()
            {
                quantity = 0;
                isUnlocked = false;
                hasSeenUnlockNotification = false;
                hasUsedFirstFree = false;
            }

            public PowerUpState(int quantity, bool isUnlocked)
            {
                this.quantity = quantity;
                this.isUnlocked = isUnlocked;
                hasSeenUnlockNotification = false;
                hasUsedFirstFree = false;
            }
        }

        /// <summary>
        /// Dictionary of power-up ID to state.
        /// </summary>
        public Dictionary<string, PowerUpState> powerUps = new();

        /// <summary>
        /// Gets the state for a power-up, creating a default if not found.
        /// </summary>
        public PowerUpState GetState(string powerUpId)
        {
            if (!powerUps.TryGetValue(powerUpId, out var state))
            {
                state = new PowerUpState();
                powerUps[powerUpId] = state;
            }
            return state;
        }

        /// <summary>
        /// Gets the quantity for a power-up.
        /// </summary>
        public int GetQuantity(string powerUpId)
        {
            return GetState(powerUpId).quantity;
        }

        /// <summary>
        /// Sets the quantity for a power-up.
        /// </summary>
        public void SetQuantity(string powerUpId, int quantity)
        {
            GetState(powerUpId).quantity = quantity;
        }

        /// <summary>
        /// Checks if a power-up is unlocked.
        /// </summary>
        public bool IsUnlocked(string powerUpId)
        {
            return GetState(powerUpId).isUnlocked;
        }

        /// <summary>
        /// Sets the unlock state for a power-up.
        /// </summary>
        public void SetUnlocked(string powerUpId, bool unlocked)
        {
            GetState(powerUpId).isUnlocked = unlocked;
        }

        /// <summary>
        /// Checks if the unlock notification has been shown.
        /// </summary>
        public bool HasSeenUnlockNotification(string powerUpId)
        {
            return GetState(powerUpId).hasSeenUnlockNotification;
        }

        /// <summary>
        /// Marks the unlock notification as shown.
        /// </summary>
        public void SetUnlockNotificationSeen(string powerUpId)
        {
            GetState(powerUpId).hasSeenUnlockNotification = true;
        }

        /// <summary>
        /// Checks if the first free use has been consumed.
        /// </summary>
        public bool HasUsedFirstFree(string powerUpId)
        {
            return GetState(powerUpId).hasUsedFirstFree;
        }

        /// <summary>
        /// Marks the first free use as consumed.
        /// </summary>
        public void SetFirstFreeUsed(string powerUpId)
        {
            GetState(powerUpId).hasUsedFirstFree = true;
        }
    }
}
