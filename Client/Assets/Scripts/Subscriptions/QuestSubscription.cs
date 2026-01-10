namespace Assets.Scripts.Shared
{
    public class QuestSubscription : AbstractSubscription<QuestSubscription, QuestSubscriptionEvent>
    {
    }

    public class QuestSubscriptionEvent : ISubscriptionEvent
    {
        public string Key { get; set; }

        public string ClientToken { get; set; }

        public string GameObjectName { get; set; }
    }
}
