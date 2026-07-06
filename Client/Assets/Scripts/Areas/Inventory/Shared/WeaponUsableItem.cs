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
            var isWearing = UserManager.Instance.Character.Weapon == Type;

            var parameters = Type.GetInventoryItemParametersAttribute();

            UserManager.Instance.Character.Strength += isWearing ? (short)(-parameters.Strength) : parameters.Strength;

            UserManager.Instance.Character.Weapon = isWearing ? InventoryItemEnum.WeaponTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Weapon, UserManager.Instance.Character.Weapon);
            GearUI.Instance.UpdateRightPanel();
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                CharacterId = 1,
                Weapon = UserManager.Instance.Character.Weapon,
                Strength = UserManager.Instance.Character.Strength,
            }, ClientToken)
            .Forget();
#endif
            return isWearing;
        }
    }
}
