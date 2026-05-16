using Assets.Scripts.Areas.Quest.Models;
using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Quest.Subscriptions
{
    public class AcceptQuestSubscription : AbstractSubscription<AcceptQuestSubscription, AddQuestSubscriptionEvent>
    {
    }

    public class AddQuestSubscriptionEvent
    {
        public CharacterQuestDto CharacterQuest { get; set; }
    }
}
