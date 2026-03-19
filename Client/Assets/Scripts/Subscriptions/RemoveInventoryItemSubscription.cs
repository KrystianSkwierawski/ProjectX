using Assets.Scripts.Models;
using Assets.Scripts.Shared;

namespace Assets.Scripts.Subscriptions
{
    public class RemoveInventoryItemSubscription : AbstractSubscription<RemoveInventoryItemSubscription, RemoveInventoryItemSubscriptionEvent>
    {
    }

    public class RemoveInventoryItemSubscriptionEvent
    {
        public InventoryItemDto Item { get; set; }

        public string ClientToken { get; set; }
    }
}
