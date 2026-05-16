namespace Assets.Scripts.Areas.Shared.Subscriptions
{
    public class SetHealthSubscription : AbstractSubscription<SetHealthSubscription, SetHealthSubscriptionEvent>
    {
    }

    public class SetHealthSubscriptionEvent
    {
        public int Value { get; set; }
    }
}
