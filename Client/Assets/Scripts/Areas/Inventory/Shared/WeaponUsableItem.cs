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
            var isWearing = character.Weapon == Type;

            var parameters = Type.GetInventoryItemParametersAttribute();

            character.Strength += isWearing ? (short)(-parameters.Strength) : parameters.Strength;

            character.Weapon = isWearing ? InventoryItemEnum.WeaponTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Weapon, character.Weapon);
            GearUI.Instance.UpdateRightPanel();
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                CharacterId = 1,
                Weapon = character.Weapon,
                Strength = character.Strength,
            }, ClientToken)
            .Forget();
#endif
            return isWearing;
        }
    }
}
