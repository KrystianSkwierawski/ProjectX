using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;

public record AcceptCharacterQuestCommand(QuestEnum QuestId) : IRequest<CharacterQuestDto>;

public class AcceptCharacterQuestCommandHandler : IRequestHandler<AcceptCharacterQuestCommand, CharacterQuestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public AcceptCharacterQuestCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, TimeProvider timeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<CharacterQuestDto> Handle(AcceptCharacterQuestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();
        var selectedCharacterId = _currentUserService.GetRequiredCharacterId();

        var character = await _context.Characters
            .Include(x => x.CharacterInventory)
            .Where(x => x.Id == selectedCharacterId)
            .Where(x => x.ApplicationUserId == userId)
            .Where(x => x.Status == StatusEnum.Active)
            .SingleOrNotFoundAsync("character", cancellationToken);

        var quest = await _context.Quests
            .Where(x => x.Id == request.QuestId)
            .Where(x => x.Status == StatusEnum.Active)
            .SingleOrNotFoundAsync("quest", cancellationToken);

        var entity = new CharacterQuest
        {
            QuestId = request.QuestId,
            CharacterId = character.Id,
            Status = CharacterQuestStatusEnum.Accepted,
            StartDate = _timeProvider.GetUtcNow()
        };

        if (quest.Type == QuestTypeEnum.Collect)
        {
            var itemType = Enum.Parse<InventoryItemEnum>(quest.GameObjectName);
            var progress = character.CharacterInventory.Inventory.GetCount(itemType);

            entity.SetProgress(progress, quest.Requirement);
        }

        _context.CharacterQuests.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return new CharacterQuestDto
        {
            Id = entity.Id,
            QuestId = entity.QuestId,
            Progress = entity.Progress,
            Status = entity.Status
        };
    }
}
