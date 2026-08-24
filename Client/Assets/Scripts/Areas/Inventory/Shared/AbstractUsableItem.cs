using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public abstract class AbstractUsableItem : IUsableItem
    {
        protected AbstractUsableItem(InventoryItemDto item, string playerSessionId, ulong ownerClientId)
        {
            Item = item;
            PlayerSessionId = playerSessionId;
            OwnerClientId = ownerClientId;
        }

        protected InventoryItemDto Item { get; }

        protected string PlayerSessionId { get; private set; }

        protected ulong OwnerClientId { get; private set; }

        public virtual void Use(UsableItemFromEnum from)
        {
#if UNITY_SERVER && !UNITY_EDITOR
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
            });
#endif
        }
    }
}
