using System.Text.Json;
using MediatR;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Inventory;

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
            .Where(inventory => inventory.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character inventory", cancellationToken);

        var dto = JsonSerializer.Deserialize<InventoryDto>(entity.Inventory);
        ArgumentNullException.ThrowIfNull(dto);

        var inventory = new InventoryState(dto.Items.Select(item => new InventorySlot(item.Type, Math.Max(0, item.Count))));

        if (request.SplitSlotIndex.HasValue)
        {
            EnsureApplied(
                inventory.Split(request.SplitSlotIndex.Value, entity.Count),
                "split inventory stack");
        }

        if (request.MoveSourceSlotIndex.HasValue && request.MoveTargetSlotIndex.HasValue)
        {
            EnsureApplied(
                inventory.Move(request.MoveSourceSlotIndex.Value, request.MoveTargetSlotIndex.Value, entity.Count),
                "move inventory item");
        }

        foreach (var item in request.Add)
        {
            EnsureApplied(inventory.Add(item.Type, item.Count), "add inventory item");
        }

        foreach (var item in request.Remove)
        {
            EnsureApplied(inventory.Remove(item.Type, item.Count), "remove inventory item");
        }

        entity.Inventory = JsonSerializer.Serialize(new InventoryDto
        {
            Items = inventory.Items
                .Select(slot => new InventoryItemDto { Type = slot.Type, Count = slot.Count })
                .ToList()
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureApplied(bool applied, string operation)
    {
        if (!applied)
        {
            throw new InvalidOperationException($"The game server requested an invalid operation: {operation}.");
        }
    }
}
