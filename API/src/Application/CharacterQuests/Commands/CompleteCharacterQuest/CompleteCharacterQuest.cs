using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;

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
            .Include(candidate => candidate.Quest)
            .Where(candidate => candidate.Id == request.CharacterQuestId)
            .Where(candidate => candidate.Status == CharacterQuestStatusEnum.Finished)
            .Where(candidate => candidate.Character.ApplicationUserId == userId)
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
        var dto = JsonSerializer.Deserialize<InventoryDto>(characterInventory.Inventory);
        ArgumentNullException.ThrowIfNull(dto);

        var inventory = new InventoryState(dto.Items.Select(item => new InventorySlot(item.Type, Math.Max(0, item.Count))));

        if (!inventory.Remove(itemType, characterQuest.Quest.Requirement))
        {
            throw new InvalidOperationException("The quest requirement is not present in the character inventory.");
        }

        characterInventory.Inventory = JsonSerializer.Serialize(new InventoryDto
        {
            Items = inventory.Items
                .Select(slot => new InventoryItemDto { Type = slot.Type, Count = slot.Count })
                .ToList()
        });
    }
}
