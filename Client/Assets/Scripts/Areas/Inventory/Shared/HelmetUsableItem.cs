using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class HelmetUsableItem : AbstractGearUsableItem
    {
        protected override InventoryItemDto GetCharacterItem(CharacterDto character) => new InventoryItemDto
        {
            Type = character.HelmetType,
            Count = 1
        };

        protected override GearSlot Slot => GearUI.Instance.Helmet;

        protected override InventoryItemEnum TemplateType => InventoryItemEnum.HelmetTemplate;

        public HelmetUsableItem(InventoryItemDto item, string playerSessionId, ulong ownerClientId) : base(item, playerSessionId, ownerClientId)
        {
        }

        protected override bool Wear(CharacterDto character)
        {
            if (character.HelmetType == Item.Type)
            {
                return false;
            }

            character.HelmetType = Item.Type;

            return true;
        }

        protected override bool Unwear(CharacterDto character)
        {
            if (character.HelmetType != Item.Type)
            {
                return false;
            }

            character.HelmetType = TemplateType;

            return true;
        }
    }
}
