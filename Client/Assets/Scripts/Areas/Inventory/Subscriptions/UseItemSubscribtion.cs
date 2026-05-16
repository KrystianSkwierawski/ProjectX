using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Inventory.Subscriptions
{
    public class UseItemSubscribtion : AbstractSubscription<UseItemSubscribtion, UseItemSubscribtionEvent>
    {
    }

    public class UseItemSubscribtionEvent
    {
        public InventoryItemDto Item { get; set; }
    }
}
