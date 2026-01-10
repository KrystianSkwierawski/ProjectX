namespace Assets.Scripts.Shared
{
    public class ExperienceSubscription : AbstractSubscription<ExperienceSubscription, ExperienceSubscriptionEvent>
    {
    }

    public class ExperienceSubscriptionEvent
    {
        public string ClientToken { get; set; }
    }
}
