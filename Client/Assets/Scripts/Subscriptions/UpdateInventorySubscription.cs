using Assets.Scripts.Models;
using Assets.Scripts.Shared;

namespace Assets.Scripts.Subscriptions
{
    public class UpdateInventorySubscription : AbstractSubscription<UpdateInventorySubscription, UpdateInventorySubscriptionEvent>
    {
    }

    public class UpdateInventorySubscriptionEvent
    {
        public UpdateCharacterInventoryCommand Request { get; set; }

        public string ClientToken { get; set; }
    }
}
