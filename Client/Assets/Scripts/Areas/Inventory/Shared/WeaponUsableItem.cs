using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class WeaponUsableItem : AbstractGearUsableItem
    {
        protected override InventoryItemDto GetCharacterItem(CharacterDto character) => new InventoryItemDto
        {
            Type = character.WeaponType,
            Count = 1
        };

        protected override GearSlot Slot => GearUI.Instance.Weapon;

        protected override InventoryItemEnum TemplateType => InventoryItemEnum.WeaponTemplate;

        public WeaponUsableItem(InventoryItemDto item, string playerSessionId, ulong ownerClientId) : base(item, playerSessionId, ownerClientId)
        {
        }

        protected override bool Wear(CharacterDto character)
        {
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

        protected override bool Unwear(CharacterDto character)
        {
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

        private void UnwearAmmo(CharacterDto character)
        {
            UnequipItems.Add(new InventoryItemDto
            {
                Type = character.AmmoType,
                Count = character.AmmoCount
            });

            character.AmmoType = InventoryItemEnum.AmmoTemplate;
            character.AmmoCount = 0;

#if !UNITY_SERVER || UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Ammo, new InventoryItemDto
            {
                Type = InventoryItemEnum.AmmoTemplate,
                Count = 0
            });
#endif
        }
    }
}
