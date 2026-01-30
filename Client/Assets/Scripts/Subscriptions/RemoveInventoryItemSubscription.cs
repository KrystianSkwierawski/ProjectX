using Assets.Scripts.Models;
using Assets.Scripts.Shared;

namespace Assets.Scripts.Subscriptions
{
    public class RemoveInventoryItemSubscription : AbstractSubscription<RemoveInventoryItemSubscription, RemoveInventoryItemSubscriptionEvent>
    {
    }

    public class RemoveInventoryItemSubscriptionEvent
    {
        public InventoryItem Item { get; set; }
    }
}
