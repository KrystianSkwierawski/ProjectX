using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Friends.Commands.RemoveFriend;

public record RemoveFriendCommand(int CharacterId) : IRequest<RemoveFriendDto>;

public class RemoveFriendCommandHandler : IRequestHandler<RemoveFriendCommand, RemoveFriendDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFriendCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<RemoveFriendDto> Handle(RemoveFriendCommand request, CancellationToken cancellationToken)
    {
        var characterId = _currentUserService.GetRequiredCharacterId();
        var firstCharacterId = Math.Min(characterId, request.CharacterId);
        var secondCharacterId = Math.Max(characterId, request.CharacterId);

        var friendship = await _context.CharacterFriendships
            .Where(x => x.FirstCharacterId == firstCharacterId)
            .Where(x => x.SecondCharacterId == secondCharacterId)
            .Where(x => x.Status == FriendshipStatusEnum.Accepted)
            .SingleOrDefaultAsync(cancellationToken);

        var characterName = await _context.Characters
            .Where(x => x.Id == request.CharacterId)
            .Select(x => x.Name)
            .SingleOrDefaultAsync(cancellationToken)
            ?? string.Empty;

        if (friendship is null)
        {
            return CreateResult(FriendOperationStatusEnum.FriendshipNotFound, request.CharacterId, characterName);
        }

        _context.CharacterFriendships.Remove(friendship);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return CreateResult(FriendOperationStatusEnum.FriendshipNotFound, request.CharacterId, characterName);
        }

        return CreateResult(FriendOperationStatusEnum.Applied, request.CharacterId, characterName);
    }

    private static RemoveFriendDto CreateResult(FriendOperationStatusEnum status, int characterId, string characterName)
    {
        return new RemoveFriendDto
        {
            Status = status,
            CharacterId = characterId,
            CharacterName = characterName
        };
    }
}
