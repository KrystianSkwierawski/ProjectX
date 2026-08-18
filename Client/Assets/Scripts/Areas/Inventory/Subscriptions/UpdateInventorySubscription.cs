using System;
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

        public bool PersistInApi { get; set; } = true;

        public bool ResynchronizeCharacterOnRejected { get; set; }

        public Action OnSucceeded { get; set; }

        public Action OnRejected { get; set; }
    }
}
