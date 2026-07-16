using System;
using System.Collections.Generic;
using System.Linq;
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

        protected readonly IList<InventoryItemDto> UnequipItems = new List<InventoryItemDto>();

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
                    UnequipItems.Add(oldItem);
                }

                foreach (var item in UnequipItems)
                {
                    RemoveStats(character, item.Type);
                }

                if (from == UsableItemFromEnum.Inventory)
                {
                    AddStats(character, Item.Type);
                }

#if UNITY_EDITOR
                AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Wear, 0.5f);

                UpdateUI(from, character);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
                UpdateCharacter(from, character);
#endif 
            }
        }

        private void UpdateCharacter(UsableItemFromEnum from, CharacterDto character)
        {
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
                    Add = UnequipItems.ToArray(),
                    Remove = from == UsableItemFromEnum.Inventory ? new InventoryItemDto[] { Item } : Array.Empty<InventoryItemDto>(),
                },
                ClientToken = ClientToken,
            });
        }

        private void UpdateUI(UsableItemFromEnum from, CharacterDto character)
        {
            GearUI.Instance.Wear(Slot, from == UsableItemFromEnum.Inventory ? Item : new InventoryItemDto
            {
                Type = TemplateType,
                Count = 0
            });

            GearUI.Instance.UpdateRightPanel();

            PlayerUI.Instance.SetMaxHealth(character.MaxHealth);
        }

        public static void AddStats(CharacterDto character, InventoryItemEnum type)
        {
            var parameters = type.GetInventoryItemParametersAttribute();

            character.MaxHealth += parameters.MaxHealth;
            character.Strength += parameters.Strength;
            character.Dexterity += parameters.Dexterity;
            character.Speed += parameters.Speed;
            character.Intellect += parameters.Intellect;
            character.Armor += parameters.Armor;
        }

        public static void RemoveStats(CharacterDto character, InventoryItemEnum type)
        {
            var parameters = type.GetInventoryItemParametersAttribute();

            character.MaxHealth -= parameters.MaxHealth;
            character.Strength -= parameters.Strength;
            character.Dexterity -= parameters.Dexterity;
            character.Speed -= parameters.Speed;
            character.Intellect -= parameters.Intellect;
            character.Armor -= parameters.Armor;
        }

        protected abstract bool Wear();

        protected abstract bool Unwear();
    }
}
