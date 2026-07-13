using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public abstract class AbstractUsableItem : IUsableItem
    {
        protected AbstractUsableItem(InventoryItemDto item, string clientToken, ulong ownerClientId)
        {
            Item = item;
            ClientToken = clientToken;
            OwnerClientId = ownerClientId;
        }

        protected InventoryItemDto Item { get; }

        protected string ClientToken { get; private set; }

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
                ClientToken = ClientToken,
            });
#endif
        }
    }
}
