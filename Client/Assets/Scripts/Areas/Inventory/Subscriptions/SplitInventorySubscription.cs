using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Inventory.Subscriptions
{
    public class SplitInventorySubscription : AbstractSubscription<SplitInventorySubscription, SplitInventorySubscriptionEvent>
    {
    }

    public class SplitInventorySubscriptionEvent
    {
        public int CharacterId { get; set; }

        public int SourceSlotIndex { get; set; }

        public string ClientToken { get; set; }
    }
}
