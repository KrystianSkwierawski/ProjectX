using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterInventories.Commands.AddCharacterInventoryItem;
public record AddCharacterInventoryItemCommand(int CharacterId, InventoryItem inventoryItem) : IRequest;

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

        ArgumentNullException.ThrowIfNull(inventory, nameof(inventory));

        inventory.Items.Add(request.inventoryItem);

        entity.Inventory = JsonSerializer.Serialize(inventory);

        await _context.SaveChangesAsync(cancellationToken);

        Log.Debug("Added item for inventory Id: {0}, Type: {1}, Count: {2}", entity.Id, request.inventoryItem.Type, request.inventoryItem.Count);
    }
}
