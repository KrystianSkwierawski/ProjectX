using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.AddCharacterQuestProgress;

public record AddCharacterQuestProgressCommand(int CharacterQuestId, int Progress) : IRequest<AddCharacterQuestProgressDto>;

public class AddCharacterQuestProgressCommandHandler : IRequestHandler<AddCharacterQuestProgressCommand, AddCharacterQuestProgressDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddCharacterQuestProgressCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AddCharacterQuestProgressDto> Handle(AddCharacterQuestProgressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();
        var selectedCharacterId = _currentUserService.GetRequiredCharacterId();

        var characterQuest = await _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.Id == request.CharacterQuestId)
            .Where(x => x.CharacterId == selectedCharacterId)
            .Where(x => x.Status == CharacterQuestStatusEnum.Accepted)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("accepted character quest", cancellationToken);

        characterQuest.AddProgress(request.Progress, characterQuest.Quest.Requirement);

        await _context.SaveChangesAsync(cancellationToken);

        return new AddCharacterQuestProgressDto
        {
            Status = characterQuest.Status,
            Reward = characterQuest.Status == CharacterQuestStatusEnum.Completed ? characterQuest.Quest.Reward : 0
        };
    }
}
