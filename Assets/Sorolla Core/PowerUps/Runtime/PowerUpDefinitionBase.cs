using UnityEngine;

namespace Sorolla.PowerUps
{
    /// <summary>
    /// Abstract base class for power-up definitions.
    /// Games should create concrete subclasses with game-specific behavior.
    /// </summary>
    public abstract class PowerUpDefinitionBase : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this power-up")]
        [SerializeField] private PowerUpId _powerUpId;

        [SerializeField] private string _displayName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Unlock Settings")]
        [Tooltip("Level required to unlock this power-up. 0 = always unlocked")]
        [Min(0)]
        [SerializeField] private int _unlockLevel;

        [Tooltip("Quantity given when first unlocked")]
        [Min(0)]
        [SerializeField] private int _initialQuantity = 3;

        [Tooltip("Maximum quantity allowed. -1 = unlimited")]
        [SerializeField] private int _maxQuantity = -1;

        [Header("Cost")]
        [SerializeField] private PowerUpCost _cost;

        /// <summary>
        /// Unique identifier for this power-up.
        /// </summary>
        public PowerUpId PowerUpId => _powerUpId;

        /// <summary>
        /// Display name shown to the player.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Description of what the power-up does.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Icon sprite for UI display.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Level required to unlock. 0 means always unlocked.
        /// </summary>
        public int UnlockLevel => _unlockLevel;

        /// <summary>
        /// Initial quantity granted when unlocked.
        /// </summary>
        public int InitialQuantity => _initialQuantity;

        /// <summary>
        /// Maximum quantity allowed. -1 for unlimited.
        /// </summary>
        public int MaxQuantity => _maxQuantity;

        /// <summary>
        /// Cost to purchase additional uses.
        /// </summary>
        public PowerUpCost Cost => _cost;

        /// <summary>
        /// Whether this power-up requires unlocking (level > 0).
        /// </summary>
        public bool RequiresUnlock => _unlockLevel > 0;

        /// <summary>
        /// Whether this power-up has a purchase cost.
        /// </summary>
        public bool HasCost => _cost.amount > 0;
    }
}
