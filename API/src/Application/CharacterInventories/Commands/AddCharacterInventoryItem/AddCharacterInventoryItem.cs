using System.Text.Json;
using System.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.CharacterInventories.Commands.AddCharacterInventoryItem;
public record AddCharacterInventoryItemCommand(int CharacterId, InventoryItemDto InventoryItem) : IRequest;

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
        using var scope = _context.CreateTransactionScope(IsolationLevel.Serializable);

        await AddAsync(request, cancellationToken);

        scope.Complete();
    }

    private async Task AddAsync(AddCharacterInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var entity = await _context.CharacterInventories
            //.Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleAsync(cancellationToken);

        Log.Debug("Found inventory for Id: {0}", entity.Id);

        var inventory = JsonSerializer.Deserialize<InventoryDto>(entity.Inventory);

        ArgumentNullException.ThrowIfNull(inventory, nameof(inventory));

        Add(request.InventoryItem, inventory);

        entity.Inventory = JsonSerializer.Serialize(inventory);

        await _context.SaveChangesAsync(cancellationToken);

        Log.Debug("Added item for inventory Id: {0}, Type: {1}, Count: {2}", entity.Id, request.InventoryItem.Type, request.InventoryItem.Count);
    }

    private static void Add(InventoryItemDto item, InventoryDto inventory)
    {
        var slot = inventory.Items
            .Where(x => x.Type == item.Type)
            .FirstOrDefault();

        // TODO: out of slots?

        if (slot == null)
        {
            inventory.Items.Add(item);

            return;

        }

        slot.Count += item.Count;
    }
}
