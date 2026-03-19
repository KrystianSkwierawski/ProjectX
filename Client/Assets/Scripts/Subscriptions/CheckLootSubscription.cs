namespace Assets.Scripts.Shared
{
    public class CheckLootSubscription : AbstractSubscription<CheckLootSubscription, CheckLootSubscriptionEvent>
    {
    }

    public class CheckLootSubscriptionEvent
    {
        public string GameObjectName { get; set; }
    }
}
