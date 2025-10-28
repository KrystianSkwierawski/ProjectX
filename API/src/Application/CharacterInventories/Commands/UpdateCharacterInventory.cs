using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterInventories.Commands;

public record UpdateCharacterInventoryCommand(int CharacterId, Inventory inventory) : IRequest;

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

        var items = JsonSerializer.Serialize(request.inventory.Items);

        await _context.CharacterInventories
            .Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(x => x.Inventory, x => items)
                    .SetProperty(x => x.ModDate, x => DateTime.Now),
                cancellationToken);
    }
}
