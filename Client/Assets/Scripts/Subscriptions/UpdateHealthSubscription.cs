namespace Assets.Scripts.Shared
{
    public class UpdateHealthSubscription : AbstractSubscription<UpdateHealthSubscription, UpdateHealthSubscriptionEvent>
    {
    }

    public class UpdateHealthSubscriptionEvent
    {
        public ulong ClientId { get; set; }

        public float Value { get; set; }

        public string ClientToken { get; set; }
    }
}
