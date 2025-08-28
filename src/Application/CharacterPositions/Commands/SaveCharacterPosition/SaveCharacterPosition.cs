using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using Serilog;

namespace ProjectX.Application.PlayerPositions.Commands.SavePlayerPosition;

public record SaveCharacterPositionCommand : IRequest
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }
}

public class SavePlayerPositionCommandHandler : IRequestHandler<SaveCharacterPositionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SavePlayerPositionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SaveCharacterPositionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.Id;

        var characterId = await _context.Character
            .Where(x => x.ApplicationUserId == userId)
            .OrderByDescending(x => x.ModDate)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);

        Log.Debug("Found character: {0} for user: {1}", characterId, userId);

        var entity = new CharacterPosition
        {
            X = request.X,
            Y = request.Y,
            Z = request.Z,
            CharacterId = characterId,
            ModDate = DateTime.Now
        };

        _context.CharacterPosition.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
