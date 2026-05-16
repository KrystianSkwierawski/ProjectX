using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Character.Subscriptions
{
    public class CheckLootSubscription : AbstractSubscription<CheckLootSubscription, CheckLootSubscriptionEvent>
    {
    }

    public class CheckLootSubscriptionEvent
    {
        public string GameObjectName { get; set; }
    }
}
