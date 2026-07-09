using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class WeaponUsableItem : AbstractGearUsableItem
    {
        public WeaponUsableItem(InventoryItemEnum type, string clientToken, ulong ownerClientId) : base(type, clientToken, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];
            var isWearing = character.WeaponType == Type;

            var parameters = Type.GetInventoryItemParametersAttribute();

            character.Strength += isWearing ? (short)(-parameters.Strength) : parameters.Strength;

            character.WeaponType = isWearing ? InventoryItemEnum.WeaponTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Weapon, character.WeaponType);
            GearUI.Instance.UpdateRightPanel();
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                CharacterId = 1,
                WeaponType = character.WeaponType,
                Strength = character.Strength,
            }, ClientToken)
            .Forget();
#endif
            return isWearing;
        }
    }
}
