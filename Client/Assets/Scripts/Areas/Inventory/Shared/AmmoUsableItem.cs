using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class AmmoUsableItem : AbstractGearUsableItem
    {
        protected override InventoryItemDto CharacterItem
        {
            get
            {
                var character = UserManager.Instance.Characters[OwnerClientId];

                return new InventoryItemDto
                {
                    Type = character.AmmoType,
                    Count = character.AmmoCount
                };
            }
        }

        protected override GearSlot Slot => GearUI.Instance.Ammo;

        protected override InventoryItemEnum TemplateType => InventoryItemEnum.AmmoTemplate;

        public AmmoUsableItem(InventoryItemDto item, string clientToken, ulong ownerClientId) : base(item, clientToken, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.AmmoType == Item.Type)
            {
                character.AmmoCount += Item.Count;

                return true;
            }

            character.AmmoType = Item.Type;
            character.AmmoCount = Item.Count;

            return true;
        }

        protected override bool Unwear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.AmmoType != Item.Type)
            {
                return false;
            }

            character.AmmoType = TemplateType;
            character.AmmoCount = 0;

            return true;
        }
    }
}
