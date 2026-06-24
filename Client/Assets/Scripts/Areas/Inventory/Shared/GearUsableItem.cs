using System;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public class GearUsableItem : AbstractUsableItem
    {
        private readonly GearTypeEnum _gearType;

        public GearUsableItem(GearTypeEnum gearType, InventoryItemEnum type, string clientToken, ulong ownerClientId) : base(type, clientToken, ownerClientId)
        {
            _gearType = gearType;
        }

        public override void Use()
        {
            var isWearing = UpdateCharacter();

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

        private bool UpdateCharacter()
        {
            bool isWearing = false;

            switch (_gearType)
            {
                case GearTypeEnum.Helmet:
                    isWearing = UserManager.Instance.Character.Helmet == Type;

                    UserManager.Instance.Character.Helmet = isWearing ? InventoryItemEnum.HelmetTemplate : Type;

#if UNITY_EDITOR
                    GearUI.Instance.Wear(GearUI.Instance.Helmet, UserManager.Instance.Character.Helmet);
#endif

                    break;
                case GearTypeEnum.Chest:
                    isWearing = UserManager.Instance.Character.Chest == Type;

                    UserManager.Instance.Character.Chest = isWearing ? InventoryItemEnum.ChestTemplate : Type;

#if UNITY_EDITOR
                    GearUI.Instance.Wear(GearUI.Instance.Chest, UserManager.Instance.Character.Chest);
#endif
                    break;
                case GearTypeEnum.Boots:
                    isWearing = UserManager.Instance.Character.Boots == Type;

                    UserManager.Instance.Character.Boots = isWearing ? InventoryItemEnum.BootsTemplate : Type;

#if UNITY_EDITOR
                    GearUI.Instance.Wear(GearUI.Instance.Boots, UserManager.Instance.Character.Boots);
#endif
                    break;
                case GearTypeEnum.Weapon:
                    isWearing = UserManager.Instance.Character.Weapon == Type;

                    UserManager.Instance.Character.Weapon = isWearing ? InventoryItemEnum.WeaponTemplate : Type;

#if UNITY_EDITOR
                    GearUI.Instance.Wear(GearUI.Instance.Weapon, UserManager.Instance.Character.Weapon);
#endif
                    break;
            }

            return isWearing;
        }
    }
}
