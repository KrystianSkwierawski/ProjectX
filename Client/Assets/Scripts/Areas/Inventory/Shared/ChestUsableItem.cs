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
            var character = UserManager.Instance.Characters[OwnerClientId];
            var isWearing = character.ChestType == Type;

            var parameters = Type.GetInventoryItemParametersAttribute();

            character.MaxHealth += isWearing ? -parameters.MaxHealth : parameters.MaxHealth;
            character.Armor += isWearing ? (short)(-parameters.Armor) : parameters.Armor;

            character.ChestType = isWearing ? InventoryItemEnum.ChestTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Chest, character.ChestType);
            GearUI.Instance.UpdateRightPanel();
            PlayerUI.Instance.SetMaxHealth(character.MaxHealth);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                CharacterId = 1,
                ChestType = character.ChestType,
                MaxHealth = character.MaxHealth,
                Armor = character.Armor,
            }, ClientToken)
            .Forget();
#endif
            return isWearing;
        }
    }
}
