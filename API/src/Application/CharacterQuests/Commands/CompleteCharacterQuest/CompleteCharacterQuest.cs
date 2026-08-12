using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.CompleteCharacterQuest;

public record CompleteCharacterQuestCommand(int CharacterQuestId) : IRequest<CompleteCharacterQuestDto>;

public class CompleteCharacterQuestCommandHandler : IRequestHandler<CompleteCharacterQuestCommand, CompleteCharacterQuestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public CompleteCharacterQuestCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, TimeProvider timeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<CompleteCharacterQuestDto> Handle(CompleteCharacterQuestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();
        var characterQuest = await _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.Id == request.CharacterQuestId)
            .Where(x => x.Status == CharacterQuestStatusEnum.Finished)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("finished character quest", cancellationToken);

        if (characterQuest.Quest.Type == QuestTypeEnum.Collect)
        {
            await CollectItemsAsync(userId, characterQuest, cancellationToken);
        }

        characterQuest.Complete(_timeProvider.GetUtcNow());
        await _context.SaveChangesAsync(cancellationToken);

        return new CompleteCharacterQuestDto { Reward = characterQuest.Quest.Reward };
    }

    private async Task CollectItemsAsync(string userId, CharacterQuest characterQuest, CancellationToken cancellationToken)
    {
        var itemType = Enum.Parse<InventoryItemEnum>(characterQuest.Quest.GameObjectName);
        var characterInventory = await _context.CharacterInventories
            .Where(inventory => inventory.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character inventory", cancellationToken);

        if (!characterInventory.Inventory.Remove(itemType, characterQuest.Quest.Requirement))
        {
            throw new InvalidOperationException("The quest requirement is not present in the character inventory.");
        }
    }
}
