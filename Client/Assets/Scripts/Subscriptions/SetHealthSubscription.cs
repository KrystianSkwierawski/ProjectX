using Assets.Scripts.Shared;

namespace Assets.Scripts.Subscriptions
{
    public class SetHealthSubscription : AbstractSubscription<SetHealthSubscription, SetHealthSubscriptionEvent>
    {
    }

    public class SetHealthSubscriptionEvent
    {
        public int Value { get; set; }
    }
}
