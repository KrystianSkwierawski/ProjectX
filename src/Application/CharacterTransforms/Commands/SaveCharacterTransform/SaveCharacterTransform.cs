using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using Serilog;

namespace ProjectX.Application.CharacterTransforms.Commands.SaveCharacterTransform;

public record SaveTransformTransformCommand : IRequest
{
    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float PositionZ { get; set; }

    public float RotationY { get; set; }

    public string ClientToken { get; set; }
}

public class SavePlayerTransformCommandHandler : IRequestHandler<SaveTransformTransformCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly TokenValidationParameters _validationParameters;

    public SavePlayerTransformCommandHandler(IApplicationDbContext context, TokenValidationParameters validationParameters)
    {
        _context = context;
        _validationParameters = validationParameters;
    }

    public async Task Handle(SaveTransformTransformCommand request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(request);

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

    private string GetUserId(SaveTransformTransformCommand request)
    {
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(request.ClientToken, _validationParameters, out var validatedToken);

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
            throw new ValidationException($"Invalid PositionX: {request.PositionX}");
        }

        if (Math.Abs(request.PositionY - last.PositionY) > 30)
        {
            throw new ValidationException($"Invalid PositionY: {request.PositionY}");
        }

        if (Math.Abs(request.PositionZ - last.PositionZ) > 30)
        {
            throw new ValidationException($"Invalid PositionZ: {request.PositionZ}");
        }
    }
}
