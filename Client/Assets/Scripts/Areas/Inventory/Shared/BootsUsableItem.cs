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
            var character = UserManager.Instance.Characters[OwnerClientId];
            var isWearing = character.BootsType == Type;

            var parameters = Type.GetInventoryItemParametersAttribute();

            character.MaxHealth += isWearing ? -parameters.MaxHealth : parameters.MaxHealth;
            character.Armor += isWearing ? (short)(-parameters.Armor) : parameters.Armor;
            character.Dexterity += isWearing ? (short)(-parameters.Dexterity) : parameters.Dexterity;
            character.Speed += isWearing ? (short)(-parameters.Speed) : parameters.Speed;

            character.BootsType = isWearing ? InventoryItemEnum.BootsTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Boots, character.BootsType);
            GearUI.Instance.UpdateRightPanel();
            PlayerUI.Instance.SetMaxHealth(character.MaxHealth);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                CharacterId = 1,
                BootsType = character.BootsType,
                MaxHealth = character.MaxHealth,
                Armor = character.Armor,
                Dexterity = character.Dexterity,
                Speed = character.Speed,
            }, ClientToken)
            .Forget();
#endif
            return isWearing;
        }
    }
}
