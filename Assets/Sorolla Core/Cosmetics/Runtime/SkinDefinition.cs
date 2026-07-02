using UnityEngine;
using Sorolla.UI;

namespace Sorolla.Cosmetics
{
    /// <summary>
    /// Game-agnostic skin metadata. Games subclass this to add their own visual
    /// payload (e.g. a snake head model). Id is a stable persistence key.
    /// </summary>
    [CreateAssetMenu(fileName = "SkinDefinition", menuName = "Sorolla/Cosmetics/Skin Definition")]
    public class SkinDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [Tooltip("Avatar sprite shown in the Competitive/Tournament UI when this skin is equipped. Optional; falls back to the profile avatar if unset.")]
        [SerializeField] private Sprite _avatar;
        [SerializeField] private SkinUnlockType _unlockType = SkinUnlockType.Default;
        [SerializeField] private int _unlockValue;
        [SerializeField, TextArea] private string _lockedDescription;
        [Tooltip("For IAP skins: the offer panel the card's BUY button opens (e.g. the bundle that grants this skin).")]
        [SerializeField] private UIPanelId _purchasePanelId;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public Sprite Avatar => _avatar;
        public SkinUnlockType UnlockType => _unlockType;
        public int UnlockValue => _unlockValue;
        public string LockedDescription => _lockedDescription;
        public UIPanelId PurchasePanelId => _purchasePanelId;
    }
}
