using Assets.Scripts.Models;
using Assets.Scripts.Shared;

namespace Assets.Scripts.Subscriptions
{
    public class PurchaseItemSubscribtion : AbstractSubscription<PurchaseItemSubscribtion, PurchaseItemSubscribtionEvent>
    {
    }

    public class PurchaseItemSubscribtionEvent
    {
        public UpdateCharacterInventoryCommand Offer { get; set; }
    }
}
