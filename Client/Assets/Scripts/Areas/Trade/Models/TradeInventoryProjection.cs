using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Trade.Models
{
    // Presentation-only reservations. Never removes items from the authoritative inventory.
    public class TradeInventoryProjection
    {
        private readonly Dictionary<int, InventoryItemDto> _reservations = new Dictionary<int, InventoryItemDto>();

        public bool IsReserved(int slotIndex) => _reservations.ContainsKey(slotIndex);

        public void PreferSource(int slotIndex, InventoryItemDto item)
        {
            if (slotIndex < 0 || item == null || item.Count <= 0)
            {
                return;
            }

            var previousCount = _reservations.TryGetValue(slotIndex, out var previous) && previous.Type == item.Type ? previous.Count : 0;
            _reservations[slotIndex] = new InventoryItemDto { Type = item.Type, Count = previousCount + item.Count };
        }

        public InventoryItemDto[] Project(IList<InventoryItemDto> inventory, IReadOnlyList<InventoryItemDto> offer)
        {
            var remaining = new Dictionary<InventoryItemEnum, long>();

            foreach (var item in offer)
            {
                remaining.TryGetValue(item.Type, out var count);
                remaining[item.Type] = count + item.Count;
            }

            // Retain the clicked source slot when several stacks share an item type.
            foreach (var index in _reservations.Keys.ToArray())
            {
                var reserved = _reservations[index];
                var source = index < inventory.Count ? inventory[index] : null;
                remaining.TryGetValue(reserved.Type, out var needed);
                var count = source != null && source.Type == reserved.Type ? (int)Math.Min(needed, Math.Min(source.Count, reserved.Count)) : 0;

                if (count <= 0)
                {
                    _reservations.Remove(index);

                    continue;
                }

                reserved.Count = count;
                remaining[reserved.Type] -= count;
            }

            var visible = new InventoryItemDto[inventory.Count];

            for (var index = 0; index < inventory.Count; index++)
            {
                var source = inventory[index];
                var type = source?.Type ?? InventoryItemEnum.None;
                var count = Math.Max(0, source?.Count ?? 0);
                var reserved = _reservations.TryGetValue(index, out var existing) ? existing.Count : 0;
                remaining.TryGetValue(type, out var needed);
                var additional = (int)Math.Min(Math.Max(0, needed), count - reserved);
                reserved += additional;

                if (reserved > 0)
                {
                    _reservations[index] = new InventoryItemDto { Type = type, Count = reserved };
                    remaining[type] -= additional;
                }

                var available = count - reserved;
                visible[index] = new InventoryItemDto { Type = available > 0 ? type : InventoryItemEnum.None, Count = available };
            }

            return visible;
        }
    }
}
