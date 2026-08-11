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
        var characterId = await _context.Characters
            .Where(character => character.ApplicationUserId == userId)
            .OrderByDescending(character => character.ModDate)
            .Select(character => (int?)character.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("character");

        _context.CharacterTransforms.Add(new CharacterTransform
        {
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            PositionZ = request.PositionZ,
            RotationY = request.RotationY,
            CharacterId = characterId
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
