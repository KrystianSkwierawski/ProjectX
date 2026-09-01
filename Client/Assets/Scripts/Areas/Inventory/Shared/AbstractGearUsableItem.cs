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
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using PartyController = Assets.Scripts.Areas.Party.Mono.Party;

namespace Assets.Scripts.Areas.Inventory.Shared
{
    public abstract class AbstractGearUsableItem : AbstractUsableItem
    {
        protected abstract InventoryItemDto CharacterItem { get; }

        protected abstract GearSlot Slot { get; }

        protected abstract InventoryItemEnum TemplateType { get; }

        protected readonly IList<InventoryItemDto> UnequipItems = new List<InventoryItemDto>();

        public AbstractGearUsableItem(InventoryItemDto item, string playerSessionId, ulong ownerClientId) : base(item, playerSessionId, ownerClientId)
        {
        }

        public override void Use(UsableItemFromEnum from)
        {
            TryUse(from);
        }

        public bool TryUse(UsableItemFromEnum from)
        {
            var character = UserManager.Instance.Characters[OwnerClientId];
            var snapshot = new CharacterSnapshot(character);
            var oldItem = CharacterItem;

            var success = from == UsableItemFromEnum.Inventory ? Wear() : Unwear();

            if (!success)
            {
                return false;
            }

            var mergesWithEquippedItem = from == UsableItemFromEnum.Inventory && oldItem.Type == Item.Type;

            if (!mergesWithEquippedItem && oldItem.Type != TemplateType)
            {
                UnequipItems.Add(oldItem);
            }

            if (!mergesWithEquippedItem)
            {
                foreach (var item in UnequipItems)
                {
                    RemoveStats(character, item.Type);
                }

                if (from == UsableItemFromEnum.Inventory)
                {
                    AddStats(character, Item.Type);
                }
            }

            var inventoryRequest = new UpdateCharacterInventoryCommand
            {
                Add = UnequipItems.ToArray(),
                Remove = from == UsableItemFromEnum.Inventory
                    ? new InventoryItemDto[] { Item }
                    : Array.Empty<InventoryItemDto>(),
            };

#if !UNITY_SERVER || UNITY_EDITOR
            if (!InventoryManager.Instance.CanApply(inventoryRequest))
            {
                snapshot.Restore(character);
                RestoreClientState();

                return false;
            }
#endif

#if UNITY_EDITOR
            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.Wear, 0.5f);

            UpdateUI(from, character);
#endif

#if UNITY_SERVER && !UNITY_EDITOR
            UpdateCharacter(character, snapshot, inventoryRequest);
#endif

            return true;
        }

        private void UpdateCharacter(
            CharacterDto character,
            CharacterSnapshot snapshot,
            UpdateCharacterInventoryCommand inventoryRequest)
        {
            UpdateInventorySubscription.Instance.Invoke(OwnerClientId.ToString(), new UpdateInventorySubscriptionEvent
            {
                Request = inventoryRequest,
                PlayerSessionId = PlayerSessionId,
                OnSucceeded = () => PersistCharacter(character),
                OnRejected = () => snapshot.Restore(character),
                ResynchronizeCharacterOnRejected = true,
            });
        }

#if !UNITY_SERVER || UNITY_EDITOR
        private static void RestoreClientState()
        {
            GearUI.Instance.UpdateLeftPanel();
            GearUI.Instance.UpdateRightPanel();
            PlayerUI.Instance.SetPlayer();

            LogUI.Instance.ShowAsync(
                TranslateManager.Instance.GetByKey(TranslateKeyEnum.InventoryFull),
                color: ColorUI.Red)
                .Forget();
        }
#endif

        private void PersistCharacter(CharacterDto character)
        {
            PartyController.NotifyCharacterChanged(OwnerClientId);

            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("Characters", new UpdateCharacterCommand
            {
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
            }, PlayerSessionId)
            .Forget();
        }

        private void UpdateUI(UsableItemFromEnum from, CharacterDto character)
        {
            GearUI.Instance.Wear(Slot, from == UsableItemFromEnum.Inventory ? CharacterItem : new InventoryItemDto
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

        private sealed class CharacterSnapshot
        {
            private readonly int _maxHealth;
            private readonly short _strength;
            private readonly short _dexterity;
            private readonly short _speed;
            private readonly short _intellect;
            private readonly short _armor;
            private readonly InventoryItemEnum _helmetType;
            private readonly InventoryItemEnum _chestType;
            private readonly InventoryItemEnum _bootsType;
            private readonly InventoryItemEnum _weaponType;
            private readonly InventoryItemEnum _ammoType;
            private readonly int _ammoCount;

            public CharacterSnapshot(CharacterDto character)
            {
                _maxHealth = character.MaxHealth;
                _strength = character.Strength;
                _dexterity = character.Dexterity;
                _speed = character.Speed;
                _intellect = character.Intellect;
                _armor = character.Armor;
                _helmetType = character.HelmetType;
                _chestType = character.ChestType;
                _bootsType = character.BootsType;
                _weaponType = character.WeaponType;
                _ammoType = character.AmmoType;
                _ammoCount = character.AmmoCount;
            }

            public void Restore(CharacterDto character)
            {
                character.MaxHealth = _maxHealth;
                character.Strength = _strength;
                character.Dexterity = _dexterity;
                character.Speed = _speed;
                character.Intellect = _intellect;
                character.Armor = _armor;
                character.HelmetType = _helmetType;
                character.ChestType = _chestType;
                character.BootsType = _bootsType;
                character.WeaponType = _weaponType;
                character.AmmoType = _ammoType;
                character.AmmoCount = _ammoCount;
            }
        }
    }
}
