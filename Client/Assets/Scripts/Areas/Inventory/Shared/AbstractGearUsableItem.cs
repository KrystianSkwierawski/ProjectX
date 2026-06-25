using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public abstract class AbstractGearUsableItem : AbstractUsableItem
    {
        public AbstractGearUsableItem(InventoryItemEnum type, string clientToken, ulong ownerClientId) : base(type, clientToken, ownerClientId)
        {
        }

        public override void Use()
        {
            var isWearing = Wear();

#if UNITY_EDITOR
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Wear, 0.5f);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
            {
                Request = new UpdateCharacterInventoryCommand
                {
                    Add = isWearing ? new InventoryItemDto[]
                    {
                        new InventoryItemDto
                        {
                            Type = Type,
                            Count = 1,
                        }
                    } : Array.Empty<InventoryItemDto>(),
                    Remove = !isWearing ? new InventoryItemDto[]
                    {
                        new InventoryItemDto
                        {
                            Type = Type,
                            Count = 1,
                        }
                    } : Array.Empty<InventoryItemDto>(),
                },
                ClientToken = ClientToken,
            });
#endif
        }

        protected abstract bool Wear();
    }
}
