using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
public record GetCharacterInventoryQuery(int CharacterId) : IRequest<CharacterInventoryDto>;

public class GetCharacterInventoryQueryHandler : IRequestHandler<GetCharacterInventoryQuery, CharacterInventoryDto>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<GetCharacterInventoryQueryHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public async Task<CharacterInventoryDto> Handle(GetCharacterInventoryQuery request, CancellationToken cancellationToken)
    {
        var result = await _context.CharacterInventories
            //.Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Character.ApplicationUserId == _currentUserService.Id)
            .Select(x => new
            {
                CharacterId = x.CharacterId,
                Items = x.Inventory,
                Count = x.Count
            })
            .SingleAsync(cancellationToken);

        var inventory = JsonSerializer.Deserialize<Inventory>(result.Items);

        ArgumentNullException.ThrowIfNull(inventory, nameof(inventory));

        return new CharacterInventoryDto
        {
            CharacterId = result.CharacterId,
            Inventory = inventory,
            Count = result.Count
        };
    }
}
