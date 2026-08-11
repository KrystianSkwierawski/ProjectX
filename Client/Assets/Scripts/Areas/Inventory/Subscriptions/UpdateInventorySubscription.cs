using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Inventory.Subscriptions
{
    public class UpdateInventorySubscription : AbstractSubscription<UpdateInventorySubscription, UpdateInventorySubscriptionEvent>
    {
    }

    public class UpdateInventorySubscriptionEvent
    {
        public UpdateCharacterInventoryCommand Request { get; set; }

        public string PlayerSessionId { get; set; }
    }
}
