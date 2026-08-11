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

        public string PlayerSessionId { get; set; }
    }

    public class MoveInventorySubscription : AbstractSubscription<MoveInventorySubscription, MoveInventorySubscriptionEvent>
    {
    }

    public class MoveInventorySubscriptionEvent
    {
        public int CharacterId { get; set; }

        public int SourceSlotIndex { get; set; }

        public int TargetSlotIndex { get; set; }

        public string PlayerSessionId { get; set; }
    }
}
