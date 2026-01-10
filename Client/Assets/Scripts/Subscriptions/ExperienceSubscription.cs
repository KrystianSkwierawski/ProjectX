namespace Assets.Scripts.Shared
{
    public class ExperienceSubscription : AbstractSubscription<ExperienceSubscription, ExperienceSubscriptionEvent>
    {
    }

    public class ExperienceSubscriptionEvent : ISubscriptionEvent
    {
        public string Key { get; set; }

        public string ClientToken { get; set; }
    }
}
