using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class WeaponUsableItem : AbstractGearUsableItem
    {
        protected override InventoryItemDto CharacterItem => new InventoryItemDto
        {
            Type = UserManager.Instance.Characters[OwnerClientId].WeaponType,
            Count = 1
        };

        protected override GearSlot Slot => GearUI.Instance.Weapon;

        protected override InventoryItemEnum TemplateType => InventoryItemEnum.WeaponTemplate;

        public WeaponUsableItem(InventoryItemDto item, string clientToken, ulong ownerClientId) : base(item, clientToken, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.WeaponType == Item.Type)
            {
                return false;
            }

            if (character.AmmoType != InventoryItemEnum.AmmoTemplate && Item.Type.GetWeaponCategory() != character.AmmoType.GetWeaponCategory())
            {
                UnwearAmmo(character);
            }

            character.WeaponType = Item.Type;

            return true;
        }

        protected override bool Unwear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.WeaponType != Item.Type)
            {
                return false;
            }

            if (character.AmmoType != InventoryItemEnum.AmmoTemplate)
            {
                UnwearAmmo(character);
            }

            character.WeaponType = TemplateType;

            return true;
        }

        private void UnwearAmmo(Character.Models.CharacterDto character)
        {
            UnequipItems.Add(new InventoryItemDto
            {
                Type = character.AmmoType,
                Count = character.AmmoCount
            });

            character.AmmoType = InventoryItemEnum.AmmoTemplate;
            character.AmmoCount = 0;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Ammo, new InventoryItemDto
            {
                Type = InventoryItemEnum.AmmoTemplate,
                Count = 0
            });
#endif
        }
    }
}
