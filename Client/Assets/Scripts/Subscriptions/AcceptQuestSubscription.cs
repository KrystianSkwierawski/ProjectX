using Assets.Scripts.Models;
using Assets.Scripts.Shared;

namespace Assets.Scripts.Subscriptions
{
    public class AcceptQuestSubscription : AbstractSubscription<AcceptQuestSubscription, AddQuestSubscriptionEvent>
    {
    }

    public class AddQuestSubscriptionEvent
    {
        public CharacterQuestDto CharacterQuest { get; set; }
    }
}
