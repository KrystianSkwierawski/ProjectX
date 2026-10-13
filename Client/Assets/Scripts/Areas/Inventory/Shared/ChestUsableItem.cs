using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class ChestUsableItem : AbstractGearUsableItem
    {
        protected override InventoryItemDto GetCharacterItem(CharacterDto character) => new InventoryItemDto
        {
            Type = character.ChestType,
            Count = 1
        };

        protected override GearSlot Slot => GearUI.Instance.Chest;

        protected override InventoryItemEnum TemplateType => InventoryItemEnum.ChestTemplate;

        public ChestUsableItem(InventoryItemDto item, string playerSessionId, ulong ownerClientId) : base(item, playerSessionId, ownerClientId)
        {
        }

        protected override bool Wear(CharacterDto character)
        {
            if (character.ChestType == Item.Type)
            {
                return false;
            }

            character.ChestType = Item.Type;

            return true;
        }

        protected override bool Unwear(CharacterDto character)
        {
            if (character.ChestType != Item.Type)
            {
                return false;
            }

            character.ChestType = TemplateType;

            return true;
        }
    }
}
