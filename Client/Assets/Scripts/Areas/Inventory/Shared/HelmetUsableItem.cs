using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class HelmetUsableItem : AbstractGearUsableItem
    {
        public HelmetUsableItem(InventoryItemEnum type, string clientToken, ulong ownerClientId) : base(type, clientToken, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var isWearing = UserManager.Instance.Character.Helmet == Type;

            var parameters = Type.GetInventoryItemParametersAttribute();

            UserManager.Instance.Character.MaxHealth += isWearing ? -parameters.MaxHealth : parameters.MaxHealth;
            UserManager.Instance.Character.Arrmor += isWearing ? (short)(-parameters.Arrmor) : parameters.Arrmor;

            UserManager.Instance.Character.Helmet = isWearing ? InventoryItemEnum.HelmetTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Helmet, UserManager.Instance.Character.Helmet);
            GearUI.Instance.UpdateRightPanel();
            PlayerUI.Instance.SetMaxHealth(UserManager.Instance.Character.MaxHealth);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                CharacterId = 1,
                Helmet = UserManager.Instance.Character.Helmet,
                MaxHealth = UserManager.Instance.Character.MaxHealth,
                Arrmor = UserManager.Instance.Character.Arrmor
            }, ClientToken)
            .Forget();
#endif
            return isWearing;
        }
    }
}
