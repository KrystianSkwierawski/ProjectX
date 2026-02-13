using Assets.Scripts.Models;

namespace Assets.Scripts.Shared
{
    public class CheckCharacterQuestSubscription : AbstractSubscription<CheckCharacterQuestSubscription, CheckCharacterQuestSubscriptionEvent>
    {
    }

    public class CheckCharacterQuestSubscriptionEvent
    {
        public int Progress { get; set; }

        public string GameObjectName { get; set; }

        public string ClientToken { get; set; }
    }
}
