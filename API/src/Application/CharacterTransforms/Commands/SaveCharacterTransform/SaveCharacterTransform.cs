using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.CharacterTransforms.Commands.SaveCharacterTransform;

public record SaveTransformTransformCommand : IRequest
{
    public float PositionX { get; init; }

    public float PositionY { get; init; }

    public float PositionZ { get; init; }

    public float RotationY { get; init; }
}

public class SavePlayerTransformCommandHandler : IRequestHandler<SaveTransformTransformCommand>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<SavePlayerTransformCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SavePlayerTransformCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SaveTransformTransformCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        int characterId = await GetCharacterIdAsync(userId, cancellationToken);

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
}
