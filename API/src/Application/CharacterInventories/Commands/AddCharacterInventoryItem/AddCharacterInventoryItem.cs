using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterInventories.Commands.AddCharacterInventoryItem;
public record AddCharacterInventoryItemCommand(int CharacterId, InventoryItemDto inventoryItem) : IRequest;

public class AddCharacterInventoryItemCommandHandler : IRequestHandler<AddCharacterInventoryItemCommand>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<AddCharacterInventoryItemCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddCharacterInventoryItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(AddCharacterInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var entity = await _context.CharacterInventories
            //.Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleAsync(cancellationToken);

        Log.Debug("Found inventory for Id: {0}", entity.Id);

        var inventory = JsonSerializer.Deserialize<InventoryDto>(entity.Inventory);

        var slot = inventory!.Items
            .Where(x => x.Type == request.inventoryItem.Type)
            .FirstOrDefault();

        if (slot == null && inventory.Items.Count >= entity.Count)
        {
            throw new Exception($"Inventory full for Id: {entity.Id}");
        }

        if (slot != null)
        {
            slot.Count += request.inventoryItem.Count;
            Log.Debug("Updated item for inventory Id: {0}, Type: {1}, New Count: {2}", entity.Id, request.inventoryItem.Type, slot.Count);

        }
        else
        {
            inventory.Items.Add(request.inventoryItem);
            Log.Debug("Created new item for inventory Id: {0}, Type: {1}, Count: {2}", entity.Id, request.inventoryItem.Type, request.inventoryItem.Count);
        }

        entity.Inventory = JsonSerializer.Serialize(inventory);

        await _context.SaveChangesAsync(cancellationToken);

        Log.Debug("Added item for inventory Id: {0}, Type: {1}, Count: {2}", entity.Id, request.inventoryItem.Type, request.inventoryItem.Count);
    }
}
