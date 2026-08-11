using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Character.Subscriptions
{
    public class AddExperienceSubscription : AbstractSubscription<AddExperienceSubscription, AddExperienceSubscriptionEvent>
    {
    }

    public class AddExperienceSubscriptionEvent
    {
        public int Amount { get; set; }

        public ExperienceTypeEnum Type { get; set; }

        public string PlayerSessionId { get; set; }
    }
}
