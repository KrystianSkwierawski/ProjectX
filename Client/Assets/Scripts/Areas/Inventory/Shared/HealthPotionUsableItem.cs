using System;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;
using PartyController = Assets.Scripts.Areas.Party.Mono.Party;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class HealthPotionUsableItem : AbstractUsableItem
    {
        public HealthPotionUsableItem(InventoryItemDto item, string playerSessionId, ulong ownerClientId) : base(item, playerSessionId, ownerClientId)
        {

        }

        public override void Use(UsableItemFromEnum from)
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (character.Health >= character.MaxHealth)
            {
                return;
            }

            character.Health = Math.Min(character.Health + 20, character.MaxHealth);
            PartyController.NotifyCharacterChanged(OwnerClientId);

#if UNITY_EDITOR
            PlayerUI.Instance.SetHealth(character.Health);

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Drinking);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
                Health = character.Health
            }, PlayerSessionId)
            .Forget();
#endif

            base.Use(from);
        }
    }

}
