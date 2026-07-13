using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class BootsUsableItem : AbstractGearUsableItem
    {
        protected override InventoryItemDto CharacterItem => new InventoryItemDto
        {
            Type = UserManager.Instance.Characters[OwnerClientId].BootsType,
            Count = 1
        };

        protected override GearSlot Slot => GearUI.Instance.Boots;

        protected override InventoryItemEnum TemplateType => InventoryItemEnum.BootsTemplate;

        public BootsUsableItem(InventoryItemDto item, string clientToken, ulong ownerClientId) : base(item, clientToken, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.BootsType == Item.Type)
            {
                return false;
            }

            character.BootsType = Item.Type;

            return true;
        }

        protected override bool Unwear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.BootsType != Item.Type)
            {
                return false;
            }

            character.BootsType = TemplateType;

            return true;
        }
    }
}