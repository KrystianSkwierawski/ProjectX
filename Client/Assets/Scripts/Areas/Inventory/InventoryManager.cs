using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Inventory
{
    public class InventoryManager : Singleton<InventoryManager>
    {
        public CharacterInventoryDto Dto { get; private set; }

        public async UniTask LoadAsync()
        {
            Dto = await UnityWebRequestHelper.ExecuteGetAsync<CharacterInventoryDto>("CharacterInventories?CharacterId=1");
        }

        public async UniTask UpdateAsync(UpdateCharacterInventoryCommand request, string playerSessionId)
        {
            await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("CharacterInventories", request, playerSessionId);
        }

        public void Add(InventoryItemDto item)
        {
            var slot = Dto.Inventory.Items
                .Where(x => x.Type == item.Type)
                .FirstOrDefault();

            if (slot == null)
            {
                var emptySlotIndex = FindEmptySlotIndex();

                if (emptySlotIndex >= 0)
                {
                    Dto.Inventory.Items[emptySlotIndex] = item;
                }
                else
                {
                    Dto.Inventory.Items.Add(item);
                }

                return;
            }

            slot.Count += item.Count;
        }

        public bool CanSplit(int sourceSlotIndex)
        {
            return Dto?.Inventory?.Items != null
                && sourceSlotIndex >= 0
                && sourceSlotIndex < Dto.Inventory.Items.Count
                && !IsEmpty(Dto.Inventory.Items[sourceSlotIndex])
                && (Dto.Inventory.Items.Count < Dto.Count || FindEmptySlotIndex() >= 0)
                && Dto.Inventory.Items[sourceSlotIndex].Count > 1;
        }

        public bool Split(int sourceSlotIndex)
        {
            if (!CanSplit(sourceSlotIndex))
            {
                return false;
            }

            var source = Dto.Inventory.Items[sourceSlotIndex];
            var splitCount = source.Count / 2;

            source.Count -= splitCount;

            var splitItem = new InventoryItemDto
            {
                Type = source.Type,
                Count = splitCount,
            };

            var emptySlotIndex = FindEmptySlotIndex();

            if (emptySlotIndex >= 0)
            {
                Dto.Inventory.Items[emptySlotIndex] = splitItem;
            }
            else
            {
                Dto.Inventory.Items.Add(splitItem);
            }

            return true;
        }

        public bool Move(int sourceSlotIndex, int targetSlotIndex)
        {
            if (Dto?.Inventory?.Items == null
                || sourceSlotIndex < 0
                || sourceSlotIndex >= Dto.Inventory.Items.Count
                || targetSlotIndex < 0
                || targetSlotIndex >= Dto.Count
                || sourceSlotIndex == targetSlotIndex
                || IsEmpty(Dto.Inventory.Items[sourceSlotIndex]))
            {
                return false;
            }

            EnsureSlotExists(targetSlotIndex);

            var source = Dto.Inventory.Items[sourceSlotIndex];
            var target = Dto.Inventory.Items[targetSlotIndex];

            if (!IsEmpty(target) && target.Type == source.Type)
            {
                target.Count += source.Count;
                Dto.Inventory.Items[sourceSlotIndex] = EmptySlot;

                return true;
            }

            Dto.Inventory.Items[sourceSlotIndex] = target;
            Dto.Inventory.Items[targetSlotIndex] = source;

            return true;
        }

        public void Remove(InventoryItemDto item)
        {
            var sum = Dto.Inventory.Items
                .Where(x => x.Type == item.Type)
                .Select(x => x.Count)
                .Sum();

            if (sum < item.Count)
            {
                throw new Exception($"Not enough items of type {item.Type} to remove. Current count: {sum}, requested count: {item.Count}");
            }

            var slot = Dto.Inventory.Items
                .Where(x => x.Type == item.Type)
                .First();

            var diff = slot.Count - item.Count;

            if (diff <= 0)
            {
                var slotIndex = Dto.Inventory.Items.IndexOf(slot);
                Dto.Inventory.Items[slotIndex] = EmptySlot;

                if (diff < 0)
                {
                    Remove(new InventoryItemDto
                    {
                        Count = Math.Abs(diff),
                        Type = item.Type
                    });
                }

                return;
            }

            slot.Count -= item.Count;
        }

        private int FindEmptySlotIndex()
        {
            for (var i = 0; i < Dto.Inventory.Items.Count; i++)
            {
                if (IsEmpty(Dto.Inventory.Items[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private void EnsureSlotExists(int slotIndex)
        {
            while (Dto.Inventory.Items.Count <= slotIndex)
            {
                Dto.Inventory.Items.Add(EmptySlot);
            }
        }

        private static bool IsEmpty(InventoryItemDto item)
        {
            return item.Type == Enums.InventoryItemEnum.None || item.Count <= 0;
        }

        private static InventoryItemDto EmptySlot => new InventoryItemDto
        {
            Type = Enums.InventoryItemEnum.None,
            Count = 0,
        };
    }
}
