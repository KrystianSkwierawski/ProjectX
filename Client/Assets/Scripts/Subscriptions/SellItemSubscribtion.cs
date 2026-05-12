using Assets.Scripts.Models;
using Assets.Scripts.Shared;

namespace Assets.Scripts.Subscriptions
{
    public class SellItemSubscribtion : AbstractSubscription<SellItemSubscribtion, SellItemSubscribtionEvent>
    {
    }

    public class SellItemSubscribtionEvent
    {
        public InventoryItemDto item { get; set; }
    }
}
