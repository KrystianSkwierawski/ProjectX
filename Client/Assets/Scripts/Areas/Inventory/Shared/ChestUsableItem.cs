using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class ChestUsableItem : AbstractGearUsableItem
    {
        public ChestUsableItem(InventoryItemEnum type, string clientToken, ulong ownerClientId) : base(type, clientToken, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var isWearing = UserManager.Instance.Character.Chest == Type;

            UserManager.Instance.Character.Chest = isWearing ? InventoryItemEnum.ChestTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Chest, UserManager.Instance.Character.Chest);
#endif
            return isWearing;
        }
    }
}
