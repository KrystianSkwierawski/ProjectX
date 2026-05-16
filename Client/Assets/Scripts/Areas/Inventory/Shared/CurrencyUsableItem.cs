using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class CurrencyUsableItem : AbstractUsableItem
    {
        public override InventoryItemEnum Type { get; } = InventoryItemEnum.Currency;

        public override void Use()
        {
#if UNITY_EDITOR
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Currency, 0.5f);
#endif
        }
    }
}
