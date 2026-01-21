namespace Assets.Scripts.Shared
{
    public class CheckLootSubscription : AbstractSubscription<CheckLootSubscription, UpdateInventorySubscriptionEvent>
    {
    }

    public class UpdateInventorySubscriptionEvent
    {
        public string ClientToken { get; set; }

        public string GameObjectName { get; set; }
    }
}
