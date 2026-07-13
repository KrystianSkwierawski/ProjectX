using System;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Subscriptions;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public abstract class AbstractGearUsableItem : AbstractUsableItem
    {
        protected abstract InventoryItemDto CharacterItem { get; }

        protected abstract GearSlot Slot { get; }

        protected abstract InventoryItemEnum TemplateType { get; }

        public AbstractGearUsableItem(InventoryItemDto item, string clientToken, ulong ownerClientId) : base(item, clientToken, ownerClientId)
        {
        }

        public override void Use(UsableItemFromEnum from)
        {
            var oldItem = CharacterItem;

            var success = from == UsableItemFromEnum.Inventory ? Wear() : Unwear();

            if (success)
            {
                var character = UserManager.Instance.Characters[OwnerClientId];

                if (oldItem.Type != TemplateType)
                {
                    var oldParameters =  oldItem.Type.GetInventoryItemParametersAttribute();

                    character.MaxHealth -= oldParameters.MaxHealth;
                    character.Strength -= oldParameters.Strength;
                    character.Dexterity -= oldParameters.Dexterity;
                    character.Speed -= oldParameters.Speed;
                    character.Intellect -= oldParameters.Intellect;
                    character.Armor -= oldParameters.Armor;
                }

                if (from == UsableItemFromEnum.Inventory)
                {
                    var parameters = Item.Type.GetInventoryItemParametersAttribute();

                    character.MaxHealth += parameters.MaxHealth;
                    character.Strength += parameters.Strength;
                    character.Dexterity += parameters.Dexterity;
                    character.Speed += parameters.Speed;
                    character.Intellect += parameters.Intellect;
                    character.Armor += parameters.Armor;
                }

#if UNITY_EDITOR
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Wear, 0.5f);

                GearUI.Instance.Wear(Slot, from == UsableItemFromEnum.Inventory ? Item : new InventoryItemDto
                {
                    Type = TemplateType,
                    Count = 0
                });

                GearUI.Instance.UpdateRightPanel();

                PlayerUI.Instance.SetMaxHealth(character.MaxHealth);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
                UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
                {
                    CharacterId = 1,
                    MaxHealth = character.MaxHealth,
                    Strength = character.Strength,
                    Dexterity = character.Dexterity,
                    Speed = character.Speed,
                    Intellect = character.Intellect,
                    Armor = character.Armor,
                    HelmetType = character.HelmetType,
                    ChestType = character.ChestType,
                    BootsType = character.BootsType,
                    WeaponType = character.WeaponType,
                    AmmoType = character.AmmoType,
                    AmmoCount = character.AmmoCount,
                }, ClientToken)
                .Forget();

                UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
                {
                    Request = new UpdateCharacterInventoryCommand
                    {
                        Add = from == UsableItemFromEnum.Gear
                            ? new InventoryItemDto[] { Item }
                            : from == UsableItemFromEnum.Inventory && oldItem.Type != TemplateType
                                ? new InventoryItemDto[] { oldItem }
                                : Array.Empty<InventoryItemDto>(),
                        Remove = from == UsableItemFromEnum.Inventory ? new InventoryItemDto[] { Item } : Array.Empty<InventoryItemDto>(),
                    },
                    ClientToken = ClientToken,
                });
#endif 
            }
        }

        protected abstract bool Wear();

        protected abstract bool Unwear();
    }
}
