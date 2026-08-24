using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public class CharacterInventoryConfiguration : IEntityTypeConfiguration<CharacterInventory>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<CharacterInventory> builder)
    {
        builder
            .Property(x => x.Inventory)
            .HasConversion(CreateConverter())
            .Metadata.SetValueComparer(CreateComparer());

        builder
            .Property(x => x.Inventory)
            .IsRequired();

        builder
            .HasOne(x => x.Character)
            .WithOne(x => x.CharacterInventory)
            .HasForeignKey<CharacterInventory>(x => x.Id);
    }

    private static ValueConverter<InventoryState, string> CreateConverter()
    {
        return new(
            inventory => Serialize(inventory),
            value => Deserialize(value));
    }

    private static ValueComparer<InventoryState> CreateComparer()
    {
        return new(
            (left, right) => HaveEqualItems(left, right),
            inventory => GetItemsHashCode(inventory),
            inventory => Copy(inventory));
    }

    private static string Serialize(InventoryState inventory)
    {
        var items = inventory.Items
            .Select(slot => new PersistedInventorySlot(slot.Type, slot.Count))
            .ToArray();

        return JsonSerializer.Serialize(new PersistedInventory(items), SerializerOptions);
    }

    private static InventoryState Deserialize(string value)
    {
        using var document = JsonDocument.Parse(value);
        var items = document.RootElement.ValueKind switch
        {
            JsonValueKind.Object => JsonSerializer.Deserialize<PersistedInventory>(value, SerializerOptions)?.Items ?? [],
            JsonValueKind.Array => JsonSerializer.Deserialize<PersistedInventorySlot[]>(value, SerializerOptions) ?? [],
            _ => throw new JsonException("The persisted inventory must be a JSON object or array.")
        };

        return new InventoryState(NormalizeSlots(items));
    }

    private static IEnumerable<InventorySlot> NormalizeSlots(IEnumerable<PersistedInventorySlot> items)
    {
        var slots = new List<InventorySlot>();
        var overflow = new Queue<InventorySlot>();

        foreach (var item in items)
        {
            if (item.Count < 0 || item.Type == InventoryItemEnum.None && item.Count > 0)
            {
                throw new JsonException("The persisted inventory contains an invalid slot.");
            }

            if (item.Count == 0)
            {
                slots.Add(InventorySlot.Empty());

                continue;
            }

            var firstStackCount = Math.Min(item.Count, InventorySlot.MaxStackSize);
            slots.Add(new InventorySlot(item.Type, firstStackCount));

            var remaining = item.Count - firstStackCount;

            while (remaining > 0)
            {
                var stackCount = Math.Min(remaining, InventorySlot.MaxStackSize);
                overflow.Enqueue(new InventorySlot(item.Type, stackCount));
                remaining -= stackCount;
            }
        }

        for (var index = 0; index < slots.Count && overflow.Count > 0; index++)
        {
            if (slots[index].IsEmpty)
            {
                slots[index] = overflow.Dequeue();
            }
        }

        slots.AddRange(overflow);

        return slots;
    }

    private static bool HaveEqualItems(InventoryState? left, InventoryState? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Items
            .Select(slot => (slot.Type, slot.Count))
            .SequenceEqual(right.Items.Select(slot => (slot.Type, slot.Count)));
    }

    private static int GetItemsHashCode(InventoryState inventory)
    {
        var hashCode = new HashCode();

        foreach (var slot in inventory.Items)
        {
            hashCode.Add(slot.Type);
            hashCode.Add(slot.Count);
        }

        return hashCode.ToHashCode();
    }

    private static InventoryState Copy(InventoryState inventory)
    {
        return new InventoryState(inventory.Items.Select(slot => new InventorySlot(slot.Type, slot.Count)));
    }

    private sealed record PersistedInventory(PersistedInventorySlot[] Items);

    private sealed record PersistedInventorySlot(InventoryItemEnum Type, int Count);
}
