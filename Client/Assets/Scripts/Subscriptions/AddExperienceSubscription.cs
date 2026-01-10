namespace Assets.Scripts.Shared
{
    public class AddExperienceSubscription : AbstractSubscription<AddExperienceSubscription, AddExperienceSubscriptionEvent>
    {
    }

    public class AddExperienceSubscriptionEvent
    {
        public string ClientToken { get; set; }
    }
}
