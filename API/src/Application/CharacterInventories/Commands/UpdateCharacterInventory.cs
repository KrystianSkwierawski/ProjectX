using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterInventories.Commands;

public record UpdateCharacterInventoryCommand(int CharacterId, Inventory inventory, string ClientToken) : IRequest;

public class UpdateCharacterInventoryCommandHandler : IRequestHandler<UpdateCharacterInventoryCommand>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<UpdateCharacterInventoryCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly TokenValidationParameters _validationParameters;

    public UpdateCharacterInventoryCommandHandler(IApplicationDbContext context, TokenValidationParameters validationParameters)
    {
        _context = context;
        _validationParameters = validationParameters;
    }

    public async Task Handle(UpdateCharacterInventoryCommand request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(request.ClientToken);

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

    private string GetUserId(string clientToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(clientToken, _validationParameters, out var validatedToken);

        var userId = principal.Claims
            .Where(x => x.Type == ClaimTypes.NameIdentifier)
            .Select(x => x.Value)
            .First();

        return userId;
    }

}
