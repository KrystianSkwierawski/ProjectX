using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Inventory;

public sealed class InventoryState
{
    private readonly List<InventorySlot> _items;

    public InventoryState(IEnumerable<InventorySlot> items)
    {
        _items = items.ToList();
    }

    public IReadOnlyList<InventorySlot> Items => _items;

    public int GetCount(InventoryItemEnum type)
    {
        return _items
            .Where(x => !x.IsEmpty)
            .Where(x => x.Type == type)
            .Sum(x => x.Count);
    }

    public InventoryState Clone()
    {
        return new InventoryState(_items.Select(x => new InventorySlot(x.Type, x.Count)));
    }

    public bool Add(InventoryItemEnum type, int count, int capacity)
    {
        if (type == InventoryItemEnum.None
            || count <= 0
            || capacity <= 0
            || _items.Count > capacity)
        {
            return false;
        }

        var availableInExistingStacks = _items
            .Where(x => !x.IsEmpty)
            .Where(x => x.Type == type)
            .Sum(x => InventorySlot.MaxStackSize - x.Count);

        var availableSlots = _items.Count(x => x.IsEmpty) + capacity - _items.Count;
        var availableCapacity = (long)availableInExistingStacks + (long)availableSlots * InventorySlot.MaxStackSize;

        if (count > availableCapacity)
        {
            return false;
        }

        var remaining = count;

        foreach (var slot in _items.Where(x => !x.IsEmpty && x.Type == type && x.Count < InventorySlot.MaxStackSize))
        {
            var added = Math.Min(InventorySlot.MaxStackSize - slot.Count, remaining);
            slot.Add(added);
            remaining -= added;

            if (remaining == 0)
            {
                return true;
            }
        }

        while (remaining > 0)
        {
            var stackCount = Math.Min(InventorySlot.MaxStackSize, remaining);
            var newSlot = new InventorySlot(type, stackCount);
            var emptySlotIndex = FindEmptySlotIndex();

            if (emptySlotIndex >= 0)
            {
                _items[emptySlotIndex] = newSlot;
            }
            else
            {
                _items.Add(newSlot);
            }

            remaining -= stackCount;
        }

        return true;
    }

    public bool Remove(InventoryItemEnum type, int count)
    {
        if (type == InventoryItemEnum.None || count <= 0)
        {
            return false;
        }

        var matchingSlots = _items.Where(x => x.Type == type).ToArray();

        if (matchingSlots.Sum(slot => slot.Count) < count)
        {
            return false;
        }

        var remaining = count;

        foreach (var slot in matchingSlots)
        {
            var removed = Math.Min(slot.Count, remaining);
            slot.Remove(removed);
            remaining -= removed;

            if (remaining == 0)
            {
                break;
            }
        }

        return true;
    }

    public bool Split(int sourceSlotIndex, int capacity)
    {
        if (!IsValidExistingSlot(sourceSlotIndex) || capacity <= 0)
        {
            return false;
        }

        var source = _items[sourceSlotIndex];

        if (source.Count < 2)
        {
            return false;
        }

        var splitCount = source.Count / 2;
        var emptySlotIndex = FindEmptySlotIndex();

        if (emptySlotIndex < 0 && _items.Count >= capacity)
        {
            return false;
        }

        source.Remove(splitCount);
        var splitSlot = new InventorySlot(source.Type, splitCount);

        if (emptySlotIndex >= 0)
        {
            _items[emptySlotIndex] = splitSlot;
        }
        else
        {
            _items.Add(splitSlot);
        }

        return true;
    }

    public bool Move(int sourceSlotIndex, int targetSlotIndex, int capacity)
    {
        if (!IsValidExistingSlot(sourceSlotIndex)
            || targetSlotIndex < 0
            || targetSlotIndex >= capacity
            || sourceSlotIndex == targetSlotIndex)
        {
            return false;
        }

        EnsureSlotExists(targetSlotIndex);

        var source = _items[sourceSlotIndex];
        var target = _items[targetSlotIndex];

        if (!target.IsEmpty && target.Type == source.Type)
        {
            var moved = Math.Min(InventorySlot.MaxStackSize - target.Count, source.Count);

            if (moved == 0)
            {
                return false;
            }

            target.Add(moved);
            source.Remove(moved);

            return true;
        }

        _items[sourceSlotIndex] = target;
        _items[targetSlotIndex] = source;
        return true;
    }

    private bool IsValidExistingSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _items.Count && !_items[slotIndex].IsEmpty;
    }

    private int FindEmptySlotIndex()
    {
        return _items.FindIndex(slot => slot.IsEmpty);
    }

    private void EnsureSlotExists(int slotIndex)
    {
        while (_items.Count <= slotIndex)
        {
            _items.Add(InventorySlot.Empty());
        }
    }
}
