using System;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class AmmoUsableItem : AbstractGearUsableItem
    {
        public AmmoUsableItem(InventoryItemEnum type, string clientToken, ulong ownerClientId) : base(type, clientToken, ownerClientId)
        {
        }

        protected override bool Wear()
        {
            var character = UserManager.Instance.Characters[OwnerClientId];
            var isWearing = character.Ammo == Type;

            var parameters = Type.GetInventoryItemParametersAttribute();

            character.Strength += isWearing ? (short)(-parameters.Strength) : parameters.Strength;
            character.Intellect += isWearing ? (short)(-parameters.Intellect) : parameters.Intellect;
            character.Armor += isWearing ? (short)(-parameters.Armor) : parameters.Armor;

            character.Ammo = isWearing ? InventoryItemEnum.AmmoTemplate : Type;

#if UNITY_EDITOR
            GearUI.Instance.Wear(GearUI.Instance.Ammo, character.Ammo);
            GearUI.Instance.UpdateRightPanel();
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                CharacterId = 1,
                Ammo = character.Ammo,
                Strength = character.Strength,
                Intellect = character.Intellect,
                Armor = character.Armor
            }, ClientToken)
            .Forget();
#endif
            return isWearing;
        }
    }
}
