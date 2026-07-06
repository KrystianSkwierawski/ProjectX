using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class BootsUsableItem : AbstractGearUsableItem
    {
        public BootsUsableItem(InventoryItemEnum type, string clientToken, ulong ownerClientId) : base(type, clientToken, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var isWearing = UserManager.Instance.Character.Boots == Type;

            var parameters = Type.GetInventoryItemParametersAttribute();

            UserManager.Instance.Character.MaxHealth += isWearing ? -parameters.MaxHealth : parameters.MaxHealth;
            UserManager.Instance.Character.Arrmor += isWearing ? (short)(-parameters.Arrmor) : parameters.Arrmor;
            UserManager.Instance.Character.Agility += isWearing ? (short)(-parameters.Agility) : parameters.Agility;
            UserManager.Instance.Character.Stamina += isWearing ? (short)(-parameters.Stamina) : parameters.Stamina;

            UserManager.Instance.Character.Boots = isWearing ? InventoryItemEnum.BootsTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Boots, UserManager.Instance.Character.Boots);
            GearUI.Instance.UpdateRightPanel();
            PlayerUI.Instance.SetMaxHealth(UserManager.Instance.Character.MaxHealth);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                CharacterId = 1,
                Boots = UserManager.Instance.Character.Boots,
                MaxHealth = UserManager.Instance.Character.MaxHealth,
                Arrmor = UserManager.Instance.Character.Arrmor,
                Agility = UserManager.Instance.Character.Agility,
                Stamina = UserManager.Instance.Character.Stamina,
            }, ClientToken)
            .Forget();
#endif
            return isWearing;
        }
    }
}
