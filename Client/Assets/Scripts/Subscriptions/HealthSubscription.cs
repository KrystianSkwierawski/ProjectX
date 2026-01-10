namespace Assets.Scripts.Shared
{
    public class HealthSubscription : AbstractSubscription<HealthSubscription, HealthSubscriptionEvent>
    {
    }

    public class HealthSubscriptionEvent : ISubscriptionEvent
    {
        public string Key { get; set; }

        public float Value { get; set; }

        public int ClientId { get; set; }

        public string ClientToken { get; set; }
    }
}
