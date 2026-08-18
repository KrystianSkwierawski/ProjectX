using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Inventory
{
    public class InventoryManager : Singleton<InventoryManager>
    {
        public const int MaxStackSize = 1024;

        public CharacterInventoryDto Dto { get; private set; }

        public async UniTask LoadAsync(int characterId)
        {
            Dto = await UnityWebRequestHelper.ExecuteGetAsync<CharacterInventoryDto>($"CharacterInventories?CharacterId={characterId}");
        }

        public async UniTask<UpdateCharacterInventoryDto> UpdateAsync(UpdateCharacterInventoryCommand request, string playerSessionId)
        {
            return await UnityWebRequestHelper.ExecutePostAsync<UpdateCharacterInventoryDto>("CharacterInventories", request, playerSessionId);
        }

        public bool CanApply(UpdateCharacterInventoryCommand request)
        {
            if (Dto?.Inventory?.Items == null || request == null)
            {
                return false;
            }

            return TryApply(CloneItems(Dto.Inventory.Items), Dto.Count, request);
        }

        public bool Apply(UpdateCharacterInventoryCommand request)
        {
            if (Dto?.Inventory?.Items == null || request == null)
            {
                return false;
            }

            var items = CloneItems(Dto.Inventory.Items);

            if (!TryApply(items, Dto.Count, request))
            {
                return false;
            }

            Dto.Inventory.Items = items;

            return true;
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
                var moved = Math.Min(MaxStackSize - target.Count, source.Count);

                if (moved == 0)
                {
                    return false;
                }

                target.Count += moved;
                source.Count -= moved;

                if (source.Count == 0)
                {
                    Dto.Inventory.Items[sourceSlotIndex] = EmptySlot;
                }

                return true;
            }

            Dto.Inventory.Items[sourceSlotIndex] = target;
            Dto.Inventory.Items[targetSlotIndex] = source;

            return true;
        }

        private static bool TryApply(
            IList<InventoryItemDto> items,
            int capacity,
            UpdateCharacterInventoryCommand request)
        {
            foreach (var item in request.Remove)
            {
                if (!TryRemove(items, item))
                {
                    return false;
                }
            }

            foreach (var item in request.Add)
            {
                if (!TryAdd(items, item, capacity))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryAdd(IList<InventoryItemDto> items, InventoryItemDto item, int capacity)
        {
            if (item == null
                || item.Type == InventoryItemEnum.None
                || item.Count <= 0
                || capacity <= 0
                || items.Count > capacity)
            {
                return false;
            }

            var availableInExistingStacks = items
                .Where(x => !IsEmpty(x))
                .Where(x => x.Type == item.Type)
                .Sum(x => MaxStackSize - x.Count);

            var availableSlots = items.Count(IsEmpty) + capacity - items.Count;
            var availableCapacity = (long)availableInExistingStacks + (long)availableSlots * MaxStackSize;

            if (item.Count > availableCapacity)
            {
                return false;
            }

            var remaining = item.Count;

            foreach (var slot in items.Where(x => !IsEmpty(x) && x.Type == item.Type && x.Count < MaxStackSize))
            {
                var added = Math.Min(MaxStackSize - slot.Count, remaining);
                slot.Count += added;
                remaining -= added;

                if (remaining == 0)
                {
                    return true;
                }
            }

            while (remaining > 0)
            {
                var stackCount = Math.Min(MaxStackSize, remaining);
                var newSlot = new InventoryItemDto
                {
                    Type = item.Type,
                    Count = stackCount
                };
                var emptySlotIndex = FindEmptySlotIndex(items);

                if (emptySlotIndex >= 0)
                {
                    items[emptySlotIndex] = newSlot;
                }
                else
                {
                    items.Add(newSlot);
                }

                remaining -= stackCount;
            }

            return true;
        }

        private static bool TryRemove(IList<InventoryItemDto> items, InventoryItemDto item)
        {
            if (item == null || item.Type == InventoryItemEnum.None || item.Count <= 0)
            {
                return false;
            }

            var matchingSlots = items.Where(x => x.Type == item.Type).ToArray();

            if (matchingSlots.Sum(x => x.Count) < item.Count)
            {
                return false;
            }

            var remaining = item.Count;

            foreach (var slot in matchingSlots)
            {
                var removed = Math.Min(slot.Count, remaining);
                slot.Count -= removed;
                remaining -= removed;

                if (slot.Count == 0)
                {
                    slot.Type = InventoryItemEnum.None;
                }

                if (remaining == 0)
                {
                    return true;
                }
            }

            return true;
        }

        private int FindEmptySlotIndex()
        {
            return FindEmptySlotIndex(Dto.Inventory.Items);
        }

        private static int FindEmptySlotIndex(IList<InventoryItemDto> items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (IsEmpty(items[i]))
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
            return item.Type == InventoryItemEnum.None || item.Count <= 0;
        }

        private static IList<InventoryItemDto> CloneItems(IEnumerable<InventoryItemDto> items)
        {
            return items
                .Select(x => new InventoryItemDto
                {
                    Type = x.Type,
                    Count = x.Count
                })
                .ToList();
        }

        private static InventoryItemDto EmptySlot => new InventoryItemDto
        {
            Type = InventoryItemEnum.None,
            Count = 0,
        };
    }
}
