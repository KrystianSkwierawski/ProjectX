using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.CheckCharacterQuestProgress;

public record CheckCharacterQuestProgressCommand(QuestEnum QuestId, int Progress, int CharacterId) : IRequest<CheckCharacterQuestProgressDto>;

public class CheckCharacterQuestProgressCommandHandler : IRequestHandler<CheckCharacterQuestProgressCommand, CheckCharacterQuestProgressDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CheckCharacterQuestProgressCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CheckCharacterQuestProgressDto> Handle(CheckCharacterQuestProgressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var characterQuest = await _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.QuestId == request.QuestId)
            .Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .Where(x => x.Status == CharacterQuestStatusEnum.Accepted)
            .FirstOrDefaultAsync(cancellationToken);

        if (characterQuest is null)
        {
            return new CheckCharacterQuestProgressDto();
        }

        characterQuest.AddProgress(request.Progress, characterQuest.Quest.Requirement);

        await _context.SaveChangesAsync(cancellationToken);

        return new CheckCharacterQuestProgressDto
        {
            QuestId = characterQuest.QuestId,
            CharacterQuestId = characterQuest.Id,
            Status = characterQuest.Status
        };
    }
}
