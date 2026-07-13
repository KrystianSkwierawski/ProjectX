using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class ChestUsableItem : AbstractGearUsableItem
    {
        protected override InventoryItemDto CharacterItem => new InventoryItemDto
        {
            Type = UserManager.Instance.Characters[OwnerClientId].ChestType,
            Count = 1
        };

        protected override GearSlot Slot => GearUI.Instance.Chest;

        protected override InventoryItemEnum TemplateType => InventoryItemEnum.ChestTemplate;

        public ChestUsableItem(InventoryItemDto item, string clientToken, ulong ownerClientId) : base(item, clientToken, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.ChestType == Item.Type)
            {
                return false;
            }

            character.ChestType = Item.Type;

            return true;
        }

        protected override bool Unwear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.ChestType != Item.Type)
            {
                return false;
            }

            character.ChestType = TemplateType;

            return true;
        }
    }
}
