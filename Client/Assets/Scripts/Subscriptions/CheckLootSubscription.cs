namespace Assets.Scripts.Shared
{
    public class CheckLootSubscription : AbstractSubscription<CheckLootSubscription, CheckLootSubscriptionEvent>
    {
    }

    public class CheckLootSubscriptionEvent
    {
        public string ClientToken { get; set; }

        public string GameObjectName { get; set; }
    }
}
