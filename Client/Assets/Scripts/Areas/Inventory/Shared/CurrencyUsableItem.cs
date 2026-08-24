using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class CurrencyUsableItem : AbstractUsableItem
    {
        public CurrencyUsableItem(InventoryItemDto item, string playerSessionId, ulong ownerClientId) : base(item, playerSessionId, ownerClientId)
        {

        }

        public override void Use(UsableItemFromEnum from)
        {
#if UNITY_EDITOR
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Currency, 0.5f);
#endif
        }
    }
}
