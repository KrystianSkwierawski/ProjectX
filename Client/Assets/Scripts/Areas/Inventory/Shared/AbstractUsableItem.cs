using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public abstract class AbstractUsableItem : IUsableItem
    {
        public abstract InventoryItemEnum Type { get; }

        protected string ClientToken { get; private set; }

        protected ulong OwnerClientId { get; private set; }

        protected CharacterDto Character { get; private set; }

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

        public IUsableItem WithClientToken(string clientToken)
        {
            ClientToken = clientToken;

            return this;
        }

        public IUsableItem WithOwnerClientId(ulong ownerClientId)
        {
            OwnerClientId = ownerClientId;

            return this;
        }

        public IUsableItem WithCharacter(CharacterDto character)
        {
            Character = character;

            return this;
        }
    }
}
