using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.CharacterTransforms.Commands.SaveCharacterTransform;

public record SaveCharacterTransformCommand : IRequest
{
    public float PositionX { get; init; }

    public float PositionY { get; init; }

    public float PositionZ { get; init; }

    public float RotationY { get; init; }
}

public class SaveCharacterTransformCommandHandler : IRequestHandler<SaveCharacterTransformCommand>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<SaveCharacterTransformCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SaveCharacterTransformCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SaveCharacterTransformCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        int characterId = await GetCharacterIdAsync(userId, cancellationToken);

        await SavePositionAsync(request, characterId, cancellationToken);
    }

    private async Task SavePositionAsync(SaveCharacterTransformCommand request, int characterId, CancellationToken cancellationToken)
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
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("character");

        Log.Debug("Found character: {0} for user: {1}", result, userId);

        return result;
    }
}
