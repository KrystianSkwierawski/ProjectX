namespace Assets.Scripts.Shared
{
    public class UpdateInventorySubscription : AbstractSubscription<UpdateInventorySubscription, UpdateInventorySubscriptionEvent>
    {
    }

    public class UpdateInventorySubscriptionEvent
    {
        public string ClientToken { get; set; }

        public string GameObjectName { get; set; }
    }
}
