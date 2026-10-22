using ProjectX.Domain.Crafting;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;

namespace ProjectX.Domain.Hideouts;

public sealed class FarmState
{
    public int Level { get; set; } = 1;

    public DateTimeOffset? UpgradeEndsAt { get; set; }

    public long Revision { get; set; }

    public FarmSlot[] Slots { get; set; } = Enumerable.Range(0, 6).Select(_ => new FarmSlot()).ToArray();

    public int Capacity => Level * 2;

    public bool Advance(DateTimeOffset now)
    {
        var changed = false;

        if (UpgradeEndsAt.HasValue && UpgradeEndsAt <= now)
        {
            Level++;
            UpgradeEndsAt = null;
            changed = true;
        }

        foreach (var slot in Slots.Take(Capacity).Where(x => x.Ready == InventoryItemEnum.None
            && x.ReadyAt.HasValue && x.ReadyAt <= now && !x.Seeds.IsEmpty))
        {
            slot.Ready = Product(slot.Seeds.Type);
            slot.Seeds.Remove(1);
            slot.ReadyAt = null;
            changed = true;
        }

        return changed;
    }

    public static InventoryItemEnum Product(InventoryItemEnum seed) => seed switch
    {
        InventoryItemEnum.ChamomileSeed => InventoryItemEnum.Chamomile,
        InventoryItemEnum.StrawberrySeed => InventoryItemEnum.Strawberry,
        InventoryItemEnum.MintSeed => InventoryItemEnum.Mint,
        InventoryItemEnum.LavenderSeed => InventoryItemEnum.Lavender,
        InventoryItemEnum.CalendulaSeed => InventoryItemEnum.Calendula,
        InventoryItemEnum.RaspberrySeed => InventoryItemEnum.Raspberry,
        _ => InventoryItemEnum.None
    };

    public static CraftingRecipeRequirement UpgradeRequirement(int level) => level switch
    {
        1 => new([new(InventoryItemEnum.Wood, 10), new(InventoryItemEnum.CopperBar, 2)], 0),
        2 => new([new(InventoryItemEnum.Wood, 20), new(InventoryItemEnum.CopperBar, 5)], 0),
        _ => new([], 0)
    };

    public static int UpgradeTime(int level) => level < 3 ? level * 30 : 0;

    public string Apply(HideoutOperationEnum operation, int slotIndex, int inventorySlot, InventoryItemEnum item,
        int count, double respawnInterval, InventoryState inventory, int capacity, DateTimeOffset now)
    {
        if (operation == HideoutOperationEnum.Upgrade)
        {
            if (Level >= 3 || UpgradeEndsAt.HasValue)
            {
                return "Unavailable";
            }

            if (!UpgradeRequirement(Level).Items.All(x => inventory.Remove(x.Type, x.Count)))
            {
                return "MissingMaterials";
            }

            UpgradeEndsAt = now.AddSeconds(UpgradeTime(Level));

            return "Applied";
        }

        if (slotIndex < 0 || slotIndex >= Capacity)
        {
            return "Unavailable";
        }

        var slot = Slots[slotIndex];

        if (operation == HideoutOperationEnum.Deposit)
        {
            if (inventorySlot < 0 || inventorySlot >= inventory.Items.Count
                || FarmState.Product(item) == InventoryItemEnum.None)
            {
                return "Unavailable";
            }

            var source = inventory.Items[inventorySlot];

            if (source.Type != item || source.Count != count)
            {
                return "StaleState";
            }

            if (!slot.Seeds.IsEmpty && slot.Seeds.Type != source.Type
                || slot.Seeds.Count + source.Count > InventorySlot.MaxStackSize)
            {
                return "InventoryFull";
            }

            slot.Seeds = new InventorySlot(source.Type, slot.Seeds.Count + source.Count);
            source.Remove(source.Count);

            if (slot.Ready == InventoryItemEnum.None && !slot.ReadyAt.HasValue)
            {
                slot.ReadyAt = now.AddSeconds(respawnInterval);
            }
        }
        else if (operation == HideoutOperationEnum.Withdraw)
        {
            if (slot.Seeds.IsEmpty)
            {
                return "Unavailable";
            }

            if (!inventory.Add(slot.Seeds.Type, slot.Seeds.Count, capacity))
            {
                return "InventoryFull";
            }

            slot.Seeds = InventorySlot.Empty();
            slot.ReadyAt = null;
        }
        else if (operation == HideoutOperationEnum.Harvest)
        {
            if (slot.Ready == InventoryItemEnum.None)
            {
                return "Unavailable";
            }

            if (!inventory.Add(slot.Ready, 1, capacity))
            {
                return "InventoryFull";
            }

            slot.Ready = InventoryItemEnum.None;
            slot.ReadyAt = slot.Seeds.IsEmpty ? null : now.AddSeconds(respawnInterval);
        }
        else
        {
            return "Unavailable";
        }

        return "Applied";
    }
}

public sealed class FarmSlot
{
    public InventorySlot Seeds { get; set; } = InventorySlot.Empty();

    public InventoryItemEnum Ready { get; set; }

    public DateTimeOffset? ReadyAt { get; set; }
}
