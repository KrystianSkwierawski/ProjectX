using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.CharacterTransforms.Commands.SaveCharacterTransform;

public record SaveTransformTransformCommand : IRequest
{
    public float PositionX { get; init; }

    public float PositionY { get; init; }

    public float PositionZ { get; init; }

    public float RotationY { get; init; }

    public required string ClientToken { get; init; }
}

public class SavePlayerTransformCommandHandler : IRequestHandler<SaveTransformTransformCommand>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<SavePlayerTransformCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly TokenValidationParameters _validationParameters;

    public SavePlayerTransformCommandHandler(IApplicationDbContext context, TokenValidationParameters validationParameters)
    {
        _context = context;
        _validationParameters = validationParameters;
    }

    public async Task Handle(SaveTransformTransformCommand request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(request.ClientToken);

        int characterId = await GetCharacterIdAsync(userId, cancellationToken);

        await ValidatePositionAsync(request, characterId);

        await SavePositionAsync(request, characterId, cancellationToken);
    }

    private async Task SavePositionAsync(SaveTransformTransformCommand request, int characterId, CancellationToken cancellationToken)
    {
        var entity = new CharacterTransform
        {
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            PositionZ = request.PositionZ,
            RotationY = request.RotationY,
            CharacterId = characterId,
            ModDate = DateTime.Now
        };

        _context.CharacterTransforms.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        Log.Debug("Saved position for character: {0}", characterId);
    }

    private async Task<int> GetCharacterIdAsync(string userId, CancellationToken cancellationToken)
    {
        var result = await _context.Characters
            .Where(x => x.ApplicationUserId == userId)
            .OrderByDescending(x => x.ModDate)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);

        Log.Debug("Found character: {0} for user: {1}", result, userId);

        return result;
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

    private async Task ValidatePositionAsync(SaveTransformTransformCommand request, int characterId)
    {
        var last = await _context.CharacterTransforms
            .Where(x => x.CharacterId == characterId)
            .OrderByDescending(x => x.ModDate)
            .Select(x => new
            {
                x.PositionX,
                x.PositionY,
                x.PositionZ
            })
            .FirstAsync();

        if (Math.Abs(request.PositionX - last.PositionX) > 30)
        {
            Log.Warning("Suspected PositionX: {0}", request.PositionX);
        }

        if (Math.Abs(request.PositionY - last.PositionY) > 30)
        {
            Log.Warning("Suspected PositionY: {0}", request.PositionY);
        }

        if (Math.Abs(request.PositionZ - last.PositionZ) > 30)
        {
            Log.Warning("Suspected PositionZ: {0}", request.PositionZ);
        }
    }
}
