using System;
using System.Threading;
using System.Threading.Tasks;
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

        // Server-state continuation for accepted persistence, including after despawn. No RPC/UI work.
        public Action OnPersisted { get; set; }

        // Await durable follow-up work under the inventory mutation's server lifetime.
        public Func<CancellationToken, Task> OnPersistedAsync { get; set; }

        public Action OnRejected { get; set; }

        public Action OnCompleted { get; set; }
    }
}
