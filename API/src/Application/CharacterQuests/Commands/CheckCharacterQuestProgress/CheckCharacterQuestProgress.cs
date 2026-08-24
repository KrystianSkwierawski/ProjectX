using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.CheckCharacterQuestProgress;

public record CheckCharacterQuestProgressCommand(QuestEnum QuestId, int Progress) : IRequest<CheckCharacterQuestProgressDto>;

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
        var selectedCharacterId = _currentUserService.GetRequiredCharacterId();

        var characterQuest = await _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.QuestId == request.QuestId)
            .Where(x => x.CharacterId == selectedCharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .Where(x => x.Status == CharacterQuestStatusEnum.Accepted || x.Status == CharacterQuestStatusEnum.Finished)
            .FirstOrDefaultAsync(cancellationToken);

        if (characterQuest is null)
        {
            return new CheckCharacterQuestProgressDto();
        }

        if (characterQuest.Quest.Type == QuestTypeEnum.Collect)
        {
            var itemType = Enum.Parse<InventoryItemEnum>(characterQuest.Quest.GameObjectName);

            var characterInventory = await _context.CharacterInventories
                .Where(x => x.Id == selectedCharacterId)
                .Where(x => x.Character.ApplicationUserId == userId)
                .SingleOrNotFoundAsync("character inventory", cancellationToken);

            var progress = characterInventory.Inventory.GetCount(itemType);

            characterQuest.SetProgress(progress, characterQuest.Quest.Requirement);
        }
        else if (characterQuest.Status == CharacterQuestStatusEnum.Accepted)
        {
            characterQuest.AddProgress(request.Progress, characterQuest.Quest.Requirement);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new CheckCharacterQuestProgressDto
        {
            QuestId = characterQuest.QuestId,
            CharacterQuestId = characterQuest.Id,
            Progress = characterQuest.Progress,
            Status = characterQuest.Status
        };
    }
}
