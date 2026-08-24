using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;

public record GetCharacterInventoryQuery(int CharacterId) : IRequest<CharacterInventoryDto>;

public class GetCharacterInventoryQueryHandler : IRequestHandler<GetCharacterInventoryQuery, CharacterInventoryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCharacterInventoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CharacterInventoryDto> Handle(GetCharacterInventoryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var result = await _context.CharacterInventories
            .Where(inventory => inventory.Id == request.CharacterId)
            .Where(inventory => inventory.Character.ApplicationUserId == userId)
            .Select(inventory => new
            {
                inventory.Id,
                inventory.Inventory,
                inventory.Count
            })
            .SingleOrNotFoundAsync("character inventory", cancellationToken);

        var effectiveCapacity = Math.Max(result.Count, result.Inventory.Items.Count);

        if (effectiveCapacity > short.MaxValue)
        {
            throw new InvalidOperationException("The character inventory exceeds the supported capacity.");
        }

        return new CharacterInventoryDto
        {
            CharacterId = result.Id,
            Inventory = new InventoryDto
            {
                Items = result.Inventory.Items
                    .Select(slot => new InventoryItemDto { Type = slot.Type, Count = slot.Count })
                    .ToList()
            },
            Count = (short)effectiveCapacity
        };
    }
}
