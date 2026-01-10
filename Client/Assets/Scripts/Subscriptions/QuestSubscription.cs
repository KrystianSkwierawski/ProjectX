namespace Assets.Scripts.Shared
{
    public class QuestSubscription : AbstractSubscription<QuestSubscription, QuestSubscriptionEvent>
    {
    }

    public class QuestSubscriptionEvent
    {
        public string ClientToken { get; set; }

        public string GameObjectName { get; set; }
    }
}
