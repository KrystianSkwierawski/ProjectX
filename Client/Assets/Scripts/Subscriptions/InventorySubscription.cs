namespace Assets.Scripts.Shared
{
    public class InventorySubscription : AbstractSubscription<InventorySubscription, InventorySubscriptionEvent>
    {
    }

    public class InventorySubscriptionEvent
    {
        public string ClientToken { get; set; }
    }
}
