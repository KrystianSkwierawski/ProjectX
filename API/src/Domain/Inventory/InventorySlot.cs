using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Inventory;

public sealed class InventorySlot
{
    public const int MaxStackSize = 1024;

    public InventorySlot(InventoryItemEnum type, int count)
    {
        if (count is < 0 or > MaxStackSize)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        Type = count == 0 ? InventoryItemEnum.None : type;
        Count = count;
    }

    public InventoryItemEnum Type { get; private set; }
    public int Count { get; private set; }

    public bool IsEmpty => Type == InventoryItemEnum.None || Count == 0;

    public void Add(int count)
    {
        if (count <= 0 || count > MaxStackSize - Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        Count += count;
    }

    public bool Remove(int count)
    {
        if (count <= 0 || count > Count)
        {
            return false;
        }

        Count -= count;

        if (Count == 0)
        {
            Type = InventoryItemEnum.None;
        }

        return true;
    }

    public static InventorySlot Empty() => new(InventoryItemEnum.None, 0);
}
