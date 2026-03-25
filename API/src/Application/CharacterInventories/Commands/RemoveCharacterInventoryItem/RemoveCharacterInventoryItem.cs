using System.Text.Json;
using System.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterInventories.Commands.RemoveCharacterInventoryItem;

public record RemoveCharacterInventoryItemCommand(int CharacterId, InventoryItemDto InventoryItem) : IRequest;

public class RemoveCharacterInventoryItemCommandHandler : IRequestHandler<RemoveCharacterInventoryItemCommand>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<RemoveCharacterInventoryItemCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveCharacterInventoryItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RemoveCharacterInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var entity = await _context.CharacterInventories
            //.Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleAsync(cancellationToken);

        Log.Debug("Found inventory for Id: {0}", entity.Id);

        var inventory = JsonSerializer.Deserialize<InventoryDto>(entity.Inventory);

        var slot = inventory!.Items
            .Where(x => x.Type == request.InventoryItem.Type)
            .Where(x => x.Count >= request.InventoryItem.Count)
            .First();

        // TODO: multiple stacks?
        if (slot.Count == request.InventoryItem.Count)
        {
            inventory.Items.Remove(slot);
        }
        else
        {
            slot.Count -= request.InventoryItem.Count;
        }

        entity.Inventory = JsonSerializer.Serialize(inventory);

        await _context.SaveChangesAsync(cancellationToken);

        Log.Debug("Removed item for inventory Id: {0}, Type: {1}, Count: {2}", entity.Id, request.InventoryItem.Type, request.InventoryItem.Count);
    }
}
