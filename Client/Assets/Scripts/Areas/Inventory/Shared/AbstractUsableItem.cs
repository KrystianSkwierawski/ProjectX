using System.Threading.Tasks;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Shared.Mono;
using UnityEngine.Networking;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public abstract class AbstractUsableItem : IUsableItem
    {
        protected AbstractUsableItem(InventoryItemEnum type, string clientToken, ulong ownerClientId)
        {
            Type = type;
            ClientToken = clientToken;
            OwnerClientId = ownerClientId;
        }

        protected InventoryItemEnum Type { get; }

        protected string ClientToken { get; private set; }

        protected ulong OwnerClientId { get; private set; }

        public virtual void Use()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
            {
                Request = new UpdateCharacterInventoryCommand
                {
                    Remove = new InventoryItemDto[]
                    {
                            new InventoryItemDto
                            {
                                Type = Type,
                                Count = 1,
                            }
                    },
                },
                ClientToken = ClientToken,
            });
#endif
        }
    }
}
