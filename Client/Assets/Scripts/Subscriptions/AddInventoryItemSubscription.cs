using Assets.Scripts.Enums;
using Assets.Scripts.Models;
using Assets.Scripts.Shared;

namespace Assets.Scripts.Subscriptions
{
    public class AddInventoryItemSubscription : AbstractSubscription<AddInventoryItemSubscription, AddInventoryItemSubscriptionEvent>
    {
    }

    public class AddInventoryItemSubscriptionEvent
    {
        public InventoryItem Item { get; set; }

        public ulong ClientId { get; set; }

        public string ClientToken { get; set; }
    }
}
