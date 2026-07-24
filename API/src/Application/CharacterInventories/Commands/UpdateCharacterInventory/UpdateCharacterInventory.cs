using System.Text.Json;
using System.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterInventories.Commands.UpdateCharacterInventory;

public record UpdateCharacterInventoryCommand(
    int CharacterId,
    InventoryItemDto[] Add,
    InventoryItemDto[] Remove,
    int? SplitSlotIndex = null,
    int? MoveSourceSlotIndex = null,
    int? MoveTargetSlotIndex = null) : IRequest;

public class UpdateCharacterInventoryCommandHandler : IRequestHandler<UpdateCharacterInventoryCommand>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<UpdateCharacterInventoryCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCharacterInventoryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateCharacterInventoryCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var entity = await _context.CharacterInventories
          //.Where(x => x.CharacterId == request.CharacterId)
          .Where(x => x.Character.ApplicationUserId == userId)
          .SingleAsync(cancellationToken);

        Log.Debug("Found inventory for Id: {0}", entity.Id);

        var inventory = JsonSerializer.Deserialize<InventoryDto>(entity.Inventory);

        ArgumentNullException.ThrowIfNull(inventory, nameof(inventory));

        if (request.SplitSlotIndex.HasValue)
        {
            var result = Split(request.SplitSlotIndex.Value, inventory, entity.Count);

            Log.Debug(
                "Split item stack for inventory Id: {0}, SlotIndex: {1}, Result: {2}",
                entity.Id,
                request.SplitSlotIndex.Value,
                result);
        }

        if (request.MoveSourceSlotIndex.HasValue && request.MoveTargetSlotIndex.HasValue)
        {
            var result = Move(
                request.MoveSourceSlotIndex.Value,
                request.MoveTargetSlotIndex.Value,
                inventory,
                entity.Count);

            Log.Debug(
                "Moved item for inventory Id: {0}, SourceSlotIndex: {1}, TargetSlotIndex: {2}, Result: {3}",
                entity.Id,
                request.MoveSourceSlotIndex.Value,
                request.MoveTargetSlotIndex.Value,
                result);
        }

        foreach (var item in request.Add)
        {
            var result = Add(item, inventory);

            Log.Debug("Added item for inventory Id: {0}, Type: {1}, Count: {2}, Result: {3}", entity.Id, item.Type, item.Count, result);
        }

        foreach (var item in request.Remove)
        {
            var result = Remove(item, inventory);

            Log.Debug("Removed item for inventory Id: {0}, Type: {1}, Count: {2}, Result: {3}", entity.Id, item.Type, item.Count, result);
        }

        entity.Inventory = JsonSerializer.Serialize(inventory);

        await _context.SaveChangesAsync(cancellationToken);

        Log.Debug("Saved inventory for Id: {0}", entity.Id);
    }

    public static bool Add(InventoryItemDto item, InventoryDto inventory)
    {
        var slot = inventory.Items
            .Where(x => x.Type == item.Type)
            .FirstOrDefault();

        if (slot == null)
        {
            var emptySlotIndex = FindEmptySlotIndex(inventory);

            if (emptySlotIndex >= 0)
            {
                inventory.Items[emptySlotIndex] = item;
            }
            else
            {
                inventory.Items.Add(item);
            }

            return true;
        }

        if (slot != null)
        {
            slot.Count += item.Count;

            return true;
        }

        // TODO: out of slots

        return false;
    }

    public static bool Split(int sourceSlotIndex, InventoryDto inventory, int capacity)
    {
        if (sourceSlotIndex < 0
            || sourceSlotIndex >= inventory.Items.Count
            || IsEmpty(inventory.Items[sourceSlotIndex]))
        {
            return false;
        }

        var source = inventory.Items[sourceSlotIndex];

        if (source.Count < 2)
        {
            return false;
        }

        var splitCount = source.Count / 2;

        source.Count -= splitCount;

        var splitItem = new InventoryItemDto
        {
            Type = source.Type,
            Count = splitCount,
        };

        var emptySlotIndex = FindEmptySlotIndex(inventory);

        if (emptySlotIndex >= 0)
        {
            inventory.Items[emptySlotIndex] = splitItem;
        }
        else if (inventory.Items.Count < capacity)
        {
            inventory.Items.Add(splitItem);
        }
        else
        {
            source.Count += splitCount;

            return false;
        }

        return true;
    }

    public static bool Move(int sourceSlotIndex, int targetSlotIndex, InventoryDto inventory, int capacity)
    {
        if (sourceSlotIndex < 0
            || sourceSlotIndex >= inventory.Items.Count
            || targetSlotIndex < 0
            || targetSlotIndex >= capacity
            || sourceSlotIndex == targetSlotIndex
            || IsEmpty(inventory.Items[sourceSlotIndex]))
        {
            return false;
        }

        EnsureSlotExists(targetSlotIndex, inventory);

        var source = inventory.Items[sourceSlotIndex];
        var target = inventory.Items[targetSlotIndex];

        if (!IsEmpty(target) && target.Type == source.Type)
        {
            target.Count += source.Count;
            inventory.Items[sourceSlotIndex] = EmptySlot;

            return true;
        }

        inventory.Items[sourceSlotIndex] = target;
        inventory.Items[targetSlotIndex] = source;

        return true;
    }

    public static bool Remove(InventoryItemDto item, InventoryDto inventory)
    {
        var slot = inventory.Items
            .Where(x => x.Type == item.Type)
            .Where(x => x.Count >= item.Count)
            .First();

        if (slot.Count == item.Count)
        {
            var slotIndex = inventory.Items.IndexOf(slot);
            inventory.Items[slotIndex] = EmptySlot;

            return true;
        }

        if (slot.Count > item.Count)
        {
            slot.Count -= item.Count;

            return true;
        }

        // TODO: multiple stacks

        return false;
    }

    private static int FindEmptySlotIndex(InventoryDto inventory)
    {
        for (var i = 0; i < inventory.Items.Count; i++)
        {
            if (IsEmpty(inventory.Items[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static void EnsureSlotExists(int slotIndex, InventoryDto inventory)
    {
        while (inventory.Items.Count <= slotIndex)
        {
            inventory.Items.Add(EmptySlot);
        }
    }

    private static bool IsEmpty(InventoryItemDto item)
    {
        return item.Type == InventoryItemEnum.None || item.Count <= 0;
    }

    private static InventoryItemDto EmptySlot => new InventoryItemDto
    {
        Type = InventoryItemEnum.None,
        Count = 0,
    };

}
