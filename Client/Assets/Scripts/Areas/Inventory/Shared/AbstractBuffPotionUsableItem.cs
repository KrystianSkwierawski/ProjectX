using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public abstract class AbstractBuffPotionUsableItem : AbstractUsableItem
    {
        private readonly ICharacterBuffController _buffController;

        protected AbstractBuffPotionUsableItem(
            InventoryItemDto item,
            string playerSessionId,
            ulong ownerClientId,
            ICharacterBuffController buffController)
            : base(item, playerSessionId, ownerClientId)
        {
            _buffController = buffController;
        }

        protected abstract float DurationSeconds { get; }

        protected abstract void SetBuff(CharacterDto character, bool active);

        public override void Use(UsableItemFromEnum from)
        {
            if (string.IsNullOrWhiteSpace(PlayerSessionId))
            {
                return;
            }

            UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
            {
                Request = new UpdateCharacterInventoryCommand
                {
                    Remove = new InventoryItemDto[]
                    {
                        Item
                    },
                },
                PlayerSessionId = PlayerSessionId,
                OnSucceeded = () => _buffController.ApplyOrRefreshBuff(Item.Type, DurationSeconds, SetBuff),
            });
        }
    }
}
