using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class HelmetUsableItem : AbstractGearUsableItem
    {
        protected override InventoryItemDto CharacterItem => new InventoryItemDto
        {
            Type = UserManager.Instance.Characters[OwnerClientId].HelmetType,
            Count = 1
        };

        protected override GearSlot Slot => GearUI.Instance.Helmet;

        protected override InventoryItemEnum TemplateType => InventoryItemEnum.HelmetTemplate;

        public HelmetUsableItem(InventoryItemDto item, string playerSessionId, ulong ownerClientId) : base(item, playerSessionId, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.HelmetType == Item.Type)
            {
                return false;
            }

            character.HelmetType = Item.Type;

            return true;
        }

        protected override bool Unwear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.HelmetType != Item.Type)
            {
                return false;
            }

            character.HelmetType = TemplateType;

            return true;
        }
    }
}
