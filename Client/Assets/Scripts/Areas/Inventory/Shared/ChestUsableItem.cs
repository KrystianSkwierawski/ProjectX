using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class ChestUsableItem : AbstractGearUsableItem
    {
        public ChestUsableItem(InventoryItemEnum type, string clientToken, ulong ownerClientId) : base(type, clientToken, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var isWearing = UserManager.Instance.Character.Chest == Type;

            var parameters = Type.GetInventoryItemParametersAttribute();

            UserManager.Instance.Character.MaxHealth += isWearing ? -parameters.MaxHealth : parameters.MaxHealth;
            UserManager.Instance.Character.Armor += isWearing ? (short)(-parameters.Armor) : parameters.Armor;

            UserManager.Instance.Character.Chest = isWearing ? InventoryItemEnum.ChestTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Chest, UserManager.Instance.Character.Chest);
            GearUI.Instance.UpdateRightPanel();
            PlayerUI.Instance.SetMaxHealth(UserManager.Instance.Character.MaxHealth);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                CharacterId = 1,
                Chest = UserManager.Instance.Character.Chest,
                MaxHealth = UserManager.Instance.Character.MaxHealth,
                Armor = UserManager.Instance.Character.Armor,
            }, ClientToken)
            .Forget();
#endif
            return isWearing;
        }
    }
}
