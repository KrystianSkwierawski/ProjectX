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
        using var scope = _context.CreateTransactionScope(IsolationLevel.Serializable);

        await RemoveAsync(request, cancellationToken);

        scope.Complete();
    }

    private async Task RemoveAsync(RemoveCharacterInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var entity = await _context.CharacterInventories
            //.Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleAsync(cancellationToken);

        Log.Debug("Found inventory for Id: {0}", entity.Id);

        var inventory = JsonSerializer.Deserialize<InventoryDto>(entity.Inventory);

        ArgumentNullException.ThrowIfNull(inventory, nameof(inventory));

        Remove(request.InventoryItem, inventory);

        entity.Inventory = JsonSerializer.Serialize(inventory);

        await _context.SaveChangesAsync(cancellationToken);

        Log.Debug("Removed item for inventory Id: {0}, Type: {1}, Count: {2}", entity.Id, request.InventoryItem.Type, request.InventoryItem.Count);
    }

    private static void Remove(InventoryItemDto item, InventoryDto inventory)
    {
        var slot = inventory.Items
            .Where(x => x.Type == item.Type)
            .Where(x => x.Count >= item.Count)
            .First();

        // TODO: multiple stacks?

        if (slot.Count == item.Count)
        {
            inventory.Items.Remove(slot);

            return;
        }

        slot.Count -= item.Count;
    }
}
