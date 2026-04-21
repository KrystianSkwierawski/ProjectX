using System.Text.Json;
using System.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterInventories.Commands.UpdateCharacterInventory;

public record UpdateCharacterInventoryCommand(int CharacterId, InventoryItemDto[] Add, InventoryItemDto[] Remove) : IRequest;

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
            inventory.Items.Add(item);

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

    public static bool Remove(InventoryItemDto item, InventoryDto inventory)
    {
        var slot = inventory.Items
            .Where(x => x.Type == item.Type)
            .Where(x => x.Count >= item.Count)
            .First();

        if (slot.Count == item.Count)
        {
            inventory.Items.Remove(slot);

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
}