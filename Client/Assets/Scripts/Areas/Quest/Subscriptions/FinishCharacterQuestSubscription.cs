using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Quest.Subscriptions
{
    public class FinishCharacterQuestSubscription : AbstractSubscription<FinishCharacterQuestSubscription, FinishCharacterQuestSubscriptionEvent>
    {
    }

    public class FinishCharacterQuestSubscriptionEvent
    {
        public bool IsFinished { get; set; }
    }
}
