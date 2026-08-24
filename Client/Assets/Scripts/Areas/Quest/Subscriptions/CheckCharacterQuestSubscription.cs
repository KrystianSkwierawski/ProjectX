using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Quest.Subscriptions
{
    public class CheckCharacterQuestSubscription : AbstractSubscription<CheckCharacterQuestSubscription, CheckCharacterQuestSubscriptionEvent>
    {
    }

    public class CheckCharacterQuestSubscriptionEvent
    {
        public int Progress { get; set; }

        public string GameObjectName { get; set; }

        public string PlayerSessionId { get; set; }
    }
}
