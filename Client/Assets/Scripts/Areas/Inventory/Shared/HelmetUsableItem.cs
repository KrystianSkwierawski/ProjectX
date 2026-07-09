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
            var character = UserManager.Instance.Characters[OwnerClientId];
            var isWearing = character.HelmetType == Type;

            var parameters = Type.GetInventoryItemParametersAttribute();

            character.MaxHealth += isWearing ? -parameters.MaxHealth : parameters.MaxHealth;
            character.Armor += isWearing ? (short)(-parameters.Armor) : parameters.Armor;

            character.HelmetType = isWearing ? InventoryItemEnum.HelmetTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Helmet, character.HelmetType);
            GearUI.Instance.UpdateRightPanel();
            PlayerUI.Instance.SetMaxHealth(character.MaxHealth);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                CharacterId = 1,
                HelmetType = character.HelmetType,
                MaxHealth = character.MaxHealth,
                Armor = character.Armor
            }, ClientToken)
            .Forget();
#endif
            return isWearing;
        }
    }
}
