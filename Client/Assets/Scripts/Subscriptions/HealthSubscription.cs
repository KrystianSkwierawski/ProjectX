namespace Assets.Scripts.Shared
{
    public class HealthSubscription : AbstractSubscription<HealthSubscription, HealthSubscriptionEvent>
    {
    }

    public class HealthSubscriptionEvent
    {
        public ulong ClientId { get; set; }

        public float Value { get; set; }

        public string ClientToken { get; set; }
    }
}
