using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
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
        var selectedCharacterId = _currentUserService.GetRequiredCharacterId();

        var character = await _context.Characters
            .Where(x => x.Id == selectedCharacterId)
            .Where(x => x.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character", cancellationToken);

        _context.CharacterTransforms.Add(new CharacterTransform
        {
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            PositionZ = request.PositionZ,
            RotationY = request.RotationY,
            CharacterId = character.Id
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
