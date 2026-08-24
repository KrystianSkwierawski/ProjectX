using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;

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

        public AmmoUsableItem(InventoryItemDto item, string playerSessionId, ulong ownerClientId) : base(item, playerSessionId, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            var weaponCategory = Item.Type.GetWeaponCategory();

            if (weaponCategory != character.WeaponType.GetWeaponCategory())
            {
#if !UNITY_SERVER || UNITY_EDITOR
                LogUI.Instance.ShowAsync
                (
                    $"{TranslateManager.Instance.GetByKey(TranslateKeyEnum.Required)}: {TranslateManager.Instance.GetByKey(weaponCategory.ToString())}", 
                    color: ColorUI.Red
                ).Forget();
#endif

                return false;
            }

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
