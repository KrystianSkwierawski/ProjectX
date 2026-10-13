using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class BootsUsableItem : AbstractGearUsableItem
    {
        protected override InventoryItemDto GetCharacterItem(CharacterDto character) => new InventoryItemDto
        {
            Type = character.BootsType,
            Count = 1
        };

        protected override GearSlot Slot => GearUI.Instance.Boots;

        protected override InventoryItemEnum TemplateType => InventoryItemEnum.BootsTemplate;

        public BootsUsableItem(InventoryItemDto item, string playerSessionId, ulong ownerClientId) : base(item, playerSessionId, ownerClientId)
        {
        }

        protected override bool Wear(CharacterDto character)
        {
            if (character.BootsType == Item.Type)
            {
                return false;
            }

            character.BootsType = Item.Type;

            return true;
        }

        protected override bool Unwear(CharacterDto character)
        {
            if (character.BootsType != Item.Type)
            {
                return false;
            }

            character.BootsType = TemplateType;

            return true;
        }
    }
}