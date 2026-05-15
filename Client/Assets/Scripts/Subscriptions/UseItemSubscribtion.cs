using Assets.Scripts.Models;
using Assets.Scripts.Shared;

namespace Assets.Scripts.Subscriptions
{
    public class UseItemSubscribtion : AbstractSubscription<UseItemSubscribtion, UseItemSubscribtionEvent>
    {
    }

    public class UseItemSubscribtionEvent
    {
        public InventoryItemDto Item { get; set; }
    }
}
