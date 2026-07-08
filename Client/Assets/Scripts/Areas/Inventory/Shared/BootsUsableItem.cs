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
            var isWearing = character.Boots == Type;

            var parameters = Type.GetInventoryItemParametersAttribute();

            character.MaxHealth += isWearing ? -parameters.MaxHealth : parameters.MaxHealth;
            character.Armor += isWearing ? (short)(-parameters.Armor) : parameters.Armor;
            character.Agility += isWearing ? (short)(-parameters.Agility) : parameters.Agility;
            character.Stamina += isWearing ? (short)(-parameters.Stamina) : parameters.Stamina;

            character.Boots = isWearing ? InventoryItemEnum.BootsTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Boots, character.Boots);
            GearUI.Instance.UpdateRightPanel();
            PlayerUI.Instance.SetMaxHealth(character.MaxHealth);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                CharacterId = 1,
                Boots = character.Boots,
                MaxHealth = character.MaxHealth,
                Armor = character.Armor,
                Agility = character.Agility,
                Stamina = character.Stamina,
            }, ClientToken)
            .Forget();
#endif
            return isWearing;
        }
    }
}
