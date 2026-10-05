using System;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
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
            TryUse(from);
        }

        public bool TryUse(UsableItemFromEnum from, Action onCompleted = null)
        {
            var character = UserManager.Instance.Characters[OwnerClientId];

            if (from != UsableItemFromEnum.Inventory || character.Health >= character.MaxHealth)
            {
                return false;
            }

            var healing = Math.Min(20, character.MaxHealth - character.Health);

#if UNITY_SERVER && !UNITY_EDITOR
            UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
            {
                Request = new UpdateCharacterInventoryCommand
                {
                    Remove = new[] { new InventoryItemDto { Type = Item.Type, Count = 1 } },
                    CharacterUpdate = new UpdateCharacterCommand { Health = character.Health + healing }
                },
                PlayerSessionId = PlayerSessionId,
                OnPersisted = () =>
                {
                    character.Health = Math.Min(character.Health + healing, character.MaxHealth);
                },
                OnSucceeded = () => PartyController.NotifyCharacterChanged(OwnerClientId),
                OnCompleted = onCompleted,
                ResynchronizeCharacterOnRejected = true
            });
#else
            character.Health += healing;
            PlayerUI.Instance.SetHealth(character.Health);
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Drinking);
#endif

            return true;
        }
    }
}
