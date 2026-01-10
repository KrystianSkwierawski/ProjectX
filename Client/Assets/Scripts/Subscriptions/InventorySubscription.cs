namespace Assets.Scripts.Shared
{
    public class InventorySubscription : AbstractSubscription<InventorySubscription, InventorySubscriptionEvent>
    {
    }

    public class InventorySubscriptionEvent : ISubscriptionEvent
    {
        public string Key { get; set; }

        public string ClientToken { get; set; }
    }
}
