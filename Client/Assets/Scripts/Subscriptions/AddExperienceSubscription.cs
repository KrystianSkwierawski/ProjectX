using Assets.Scripts.Enums;

namespace Assets.Scripts.Shared
{
    public class AddExperienceSubscription : AbstractSubscription<AddExperienceSubscription, AddExperienceSubscriptionEvent>
    {
    }

    public class AddExperienceSubscriptionEvent
    {
        public int Amount { get; set; }

        public ExperienceTypeEnum Type { get; set; }

        public string ClientToken { get; set; }
    }
}
