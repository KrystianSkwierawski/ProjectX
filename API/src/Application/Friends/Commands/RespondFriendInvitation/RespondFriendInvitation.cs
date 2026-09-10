using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Friends.Commands.RespondFriendInvitation;

public record RespondFriendInvitationCommand : IRequest<RespondFriendInvitationDto>
{
    public int CharacterId { get; init; }

    public bool Accept { get; init; }
}

public class RespondFriendInvitationCommandHandler : IRequestHandler<RespondFriendInvitationCommand, RespondFriendInvitationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RespondFriendInvitationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<RespondFriendInvitationDto> Handle(RespondFriendInvitationCommand request, CancellationToken cancellationToken)
    {
        var characterId = _currentUserService.GetRequiredCharacterId();
        var firstCharacterId = Math.Min(characterId, request.CharacterId);
        var secondCharacterId = Math.Max(characterId, request.CharacterId);

        var invitation = await _context.CharacterFriendships
            .Where(x => x.FirstCharacterId == firstCharacterId)
            .Where(x => x.SecondCharacterId == secondCharacterId)
            .Where(x => x.Status == FriendshipStatusEnum.Pending)
            .Where(x => x.RequestedByCharacterId == request.CharacterId)
            .SingleOrDefaultAsync(cancellationToken);

        var characterName = await GetCharacterNameAsync(request.CharacterId, cancellationToken);

        if (invitation is null)
        {
            return CreateResult(FriendOperationStatusEnum.InvitationNotFound, request.CharacterId, characterName);
        }

        if (request.Accept)
        {
            invitation.Accept(characterId);
        }
        else
        {
            _context.CharacterFriendships.Remove(invitation);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return CreateResult(FriendOperationStatusEnum.InvitationNotFound, request.CharacterId, characterName);
        }

        return CreateResult(FriendOperationStatusEnum.Applied, request.CharacterId, characterName);
    }

    private async Task<string> GetCharacterNameAsync(int characterId, CancellationToken cancellationToken)
    {
        return await _context.Characters
            .Where(x => x.Id == characterId)
            .Select(x => x.Name)
            .SingleOrDefaultAsync(cancellationToken)
            ?? string.Empty;
    }

    private static RespondFriendInvitationDto CreateResult(FriendOperationStatusEnum status, int characterId, string characterName)
    {
        return new RespondFriendInvitationDto
        {
            Status = status,
            CharacterId = characterId,
            CharacterName = characterName
        };
    }
}
