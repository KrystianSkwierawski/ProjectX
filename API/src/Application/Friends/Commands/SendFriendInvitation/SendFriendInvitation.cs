using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Friends.Commands.SendFriendInvitation;

public record SendFriendInvitationCommand : IRequest<SendFriendInvitationDto>
{
    public required string CharacterName { get; init; }
}

public class SendFriendInvitationCommandHandler : IRequestHandler<SendFriendInvitationCommand, SendFriendInvitationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SendFriendInvitationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<SendFriendInvitationDto> Handle(SendFriendInvitationCommand request, CancellationToken cancellationToken)
    {
        var characterId = _currentUserService.GetRequiredCharacterId();
        var characterName = request.CharacterName.Trim();

        var target = await _context.Characters
            .Where(x => x.Name == characterName)
            .Where(x => x.Status == StatusEnum.Active)
            .Select(x => new { x.Id, x.Name })
            .SingleOrDefaultAsync(cancellationToken);

        if (target is null)
        {
            return new SendFriendInvitationDto { Status = FriendOperationStatusEnum.CharacterNotFound };
        }

        if (target.Id == characterId)
        {
            return CreateResult(FriendOperationStatusEnum.CannotInviteSelf, target.Id, target.Name);
        }

        var firstCharacterId = Math.Min(characterId, target.Id);
        var secondCharacterId = Math.Max(characterId, target.Id);

        var existing = await _context.CharacterFriendships
            .Where(x => x.FirstCharacterId == firstCharacterId)
            .Where(x => x.SecondCharacterId == secondCharacterId)
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return CreateResult(GetExistingStatus(existing), target.Id, target.Name);
        }

        _context.CharacterFriendships.Add(CharacterFriendship.Create(characterId, target.Id));

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var concurrentFriendship = await _context.CharacterFriendships
                .AsNoTracking()
                .Where(x => x.FirstCharacterId == firstCharacterId)
                .Where(x => x.SecondCharacterId == secondCharacterId)
                .SingleOrDefaultAsync(cancellationToken);

            if (concurrentFriendship is null)
            {
                throw;
            }

            return CreateResult(GetExistingStatus(concurrentFriendship), target.Id, target.Name);
        }

        return CreateResult(FriendOperationStatusEnum.Applied, target.Id, target.Name);
    }

    private static FriendOperationStatusEnum GetExistingStatus(CharacterFriendship friendship)
    {
        return friendship.Status == FriendshipStatusEnum.Accepted
            ? FriendOperationStatusEnum.AlreadyFriends
            : FriendOperationStatusEnum.InvitationAlreadyPending;
    }

    private static SendFriendInvitationDto CreateResult(FriendOperationStatusEnum status, int characterId, string characterName)
    {
        return new SendFriendInvitationDto
        {
            Status = status,
            CharacterId = characterId,
            CharacterName = characterName
        };
    }
}
