using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.PowerUps
{
    /// <summary>
    /// Registry of all available power-ups in the game.
    /// Assign this to PowerUpService to initialize available power-ups.
    /// </summary>
    [CreateAssetMenu(fileName = "PowerUpRegistry", menuName = "Sorolla/Power-Ups/Registry")]
    public class PowerUpRegistry : ScriptableObject
    {
        [Tooltip("List of all power-up definitions available in the game")]
        [SerializeField] private List<PowerUpDefinitionBase> _powerUps = new();

        /// <summary>
        /// All registered power-up definitions.
        /// </summary>
        public IReadOnlyList<PowerUpDefinitionBase> PowerUps => _powerUps;

        /// <summary>
        /// Gets a power-up definition by ID.
        /// </summary>
        public PowerUpDefinitionBase GetById(PowerUpId powerUpId)
        {
            foreach (var powerUp in _powerUps)
            {
                if (powerUp != null && powerUp.PowerUpId == powerUpId)
                {
                    return powerUp;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all power-ups that should be unlocked at or before a given level.
        /// </summary>
        public IEnumerable<PowerUpDefinitionBase> GetPowerUpsUnlockedAtLevel(int level)
        {
            foreach (var powerUp in _powerUps)
            {
                if (powerUp != null && powerUp.UnlockLevel <= level)
                {
                    yield return powerUp;
                }
            }
        }

        /// <summary>
        /// Gets all power-ups that require unlocking (unlock level > 0).
        /// </summary>
        public IEnumerable<PowerUpDefinitionBase> GetLockablePowerUps()
        {
            foreach (var powerUp in _powerUps)
            {
                if (powerUp != null && powerUp.RequiresUnlock)
                {
                    yield return powerUp;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Remove null entries and check for duplicate IDs
            var seenIds = new HashSet<PowerUpId>();
            for (int i = _powerUps.Count - 1; i >= 0; i--)
            {
                if (_powerUps[i] == null)
                {
                    _powerUps.RemoveAt(i);
                    continue;
                }

                var id = _powerUps[i].PowerUpId;
                if (seenIds.Contains(id))
                {
                    Debug.LogWarning($"[PowerUpRegistry] Duplicate power-up ID: {id}");
                }
                seenIds.Add(id);
            }
        }
#endif
    }
}
