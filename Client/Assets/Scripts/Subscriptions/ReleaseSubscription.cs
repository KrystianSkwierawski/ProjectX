namespace Assets.Scripts.Shared
{
    public class ReleaseSubscription : AbstractSubscription<ReleaseSubscription, ReleaseSubscriptionEvent>
    {
    }

    public class ReleaseSubscriptionEvent : ISubscriptionEvent
    {
        public string Key { get; set; }
    }
}
