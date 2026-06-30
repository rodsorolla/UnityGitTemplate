using Sorolla.UI;
using Sorolla.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Tournaments.UI
{
    /// One reward entry inside a ChestRewardBubble — icon + amount. Prefab authored by the
    /// user (RewardBulle); fields assigned in the inspector. Icon is resolved through the
    /// central IIconResolver, same as the other data-driven LiveOps surfaces.
    public class RewardBulleView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;

        public void Bind(EventReward reward, IIconResolver icons)
        {
            if (reward == null) return;
            if (_icon != null) _icon.sprite = icons?.Resolve(reward.ItemType, reward.ItemId);
            if (_amount != null) _amount.text = "x" + reward.Amount;
        }
    }
}
