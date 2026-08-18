using MediatR;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterInventories.Commands.UpdateCharacterInventory;

public record UpdateCharacterInventoryCommand(
    InventoryItemDto[] Add,
    InventoryItemDto[] Remove,
    int? SplitSlotIndex = null,
    int? MoveSourceSlotIndex = null,
    int? MoveTargetSlotIndex = null) : IRequest<UpdateCharacterInventoryDto>;

public class UpdateCharacterInventoryCommandHandler : IRequestHandler<UpdateCharacterInventoryCommand, UpdateCharacterInventoryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCharacterInventoryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateCharacterInventoryDto> Handle(UpdateCharacterInventoryCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();
        var selectedCharacterId = _currentUserService.GetRequiredCharacterId();

        var entity = await _context.CharacterInventories
            .Where(x => x.Id == selectedCharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character inventory", cancellationToken);

        var inventory = entity.Inventory.Clone();
        var effectiveCapacity = Math.Max(entity.Count, inventory.Items.Count);

        if (effectiveCapacity > short.MaxValue)
        {
            throw new InvalidOperationException("The character inventory exceeds the supported capacity.");
        }

        if (request.SplitSlotIndex.HasValue)
        {
            EnsureApplied(
                inventory.Split(request.SplitSlotIndex.Value, effectiveCapacity),
                "split inventory stack");
        }

        if (request.MoveSourceSlotIndex.HasValue && request.MoveTargetSlotIndex.HasValue)
        {
            EnsureApplied(
                inventory.Move(request.MoveSourceSlotIndex.Value, request.MoveTargetSlotIndex.Value, effectiveCapacity),
                "move inventory item");
        }

        foreach (var item in request.Remove)
        {
            EnsureValidItem(item, "remove inventory item");
            EnsureApplied(inventory.Remove(item.Type, item.Count), "remove inventory item");
        }

        foreach (var item in request.Add)
        {
            EnsureValidItem(item, "add inventory item");

            if (!inventory.Add(item.Type, item.Count, effectiveCapacity))
            {
                return new UpdateCharacterInventoryDto
                {
                    Status = UpdateCharacterInventoryStatusEnum.InventoryFull
                };
            }
        }

        entity.Inventory = inventory;
        entity.Count = (short)effectiveCapacity;

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateCharacterInventoryDto
        {
            Status = UpdateCharacterInventoryStatusEnum.Applied
        };
    }

    private static void EnsureValidItem(InventoryItemDto item, string operation)
    {
        if (item.Type == InventoryItemEnum.None || item.Count <= 0)
        {
            throw new InvalidOperationException($"The game server requested an invalid operation: {operation}.");
        }
    }

    private static void EnsureApplied(bool applied, string operation)
    {
        if (!applied)
        {
            throw new InvalidOperationException($"The game server requested an invalid operation: {operation}.");
        }
    }
}
