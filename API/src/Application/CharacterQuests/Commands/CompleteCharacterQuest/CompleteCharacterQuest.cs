using MediatR;
using ProjectX.Application.CharacterQuests.Commands.CheckCharacterQuestProgress;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Characters;
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
        var selectedCharacterId = _currentUserService.GetRequiredCharacterId();

        var characterQuest = await _context.CharacterQuests
            .Include(x => x.Quest)
            .Include(x => x.Character)
                .ThenInclude(x => x.CharacterExperiences.Where(experience => experience.Type == ExperienceTypeEnum.Main))
            .Where(x => x.Id == request.CharacterQuestId)
            .Where(x => x.CharacterId == selectedCharacterId)
            .Where(x => x.Status == CharacterQuestStatusEnum.Finished)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("finished character quest", cancellationToken);

        var collectProgress = new List<CheckCharacterQuestProgressDto>();

        var inventoryStatus = await ApplyInventoryAsync(userId, selectedCharacterId, characterQuest, collectProgress, cancellationToken);

        if (inventoryStatus != CompleteCharacterQuestStatusEnum.Applied)
        {
            return new CompleteCharacterQuestDto { QuestId = characterQuest.QuestId, Status = inventoryStatus };
        }

        characterQuest.Complete(_timeProvider.GetUtcNow());

        characterQuest.Character.CharacterExperiences.Add(new CharacterExperience
        {
            Amount = characterQuest.QuestId.GetExperienceReward(),
            Type = ExperienceTypeEnum.Main
        });

        var experience = characterQuest.Character.CharacterExperiences
            .Where(entry => entry.Type == ExperienceTypeEnum.Main)
            .Sum(entry => entry.Amount);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new CompleteCharacterQuestDto
            {
                QuestId = characterQuest.QuestId,
                Status = CompleteCharacterQuestStatusEnum.InventoryChanged
            };
        }

        return new CompleteCharacterQuestDto
        {
            QuestId = characterQuest.QuestId,
            Status = CompleteCharacterQuestStatusEnum.Applied,
            Reward = characterQuest.QuestId.GetExperienceReward(),
            Level = ExperienceProgression.GetLevel(experience),
            CollectProgress = collectProgress.ToArray()
        };
    }

    private async Task<CompleteCharacterQuestStatusEnum> ApplyInventoryAsync(
        string userId,
        int selectedCharacterId,
        CharacterQuest characterQuest,
        List<CheckCharacterQuestProgressDto> collectProgress,
        CancellationToken cancellationToken)
    {
        var rewards = characterQuest.QuestId.GetItemRewards();

        var requirements = characterQuest.QuestId.GetRequirements();

        var characterInventory = await _context.CharacterInventories
            .Where(x => x.Id == selectedCharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character inventory", cancellationToken);

        var inventory = characterInventory.Inventory.Clone();
        var capacity = Math.Max(characterInventory.Count, inventory.Items.Count);

        if (requirements.Any(x => (x.Type == QuestTypeEnum.Collect
                ? inventory.GetCount(x.ItemType) : characterQuest.ObjectiveProgress.GetValueOrDefault(x.Id)) < x.Count))
        {
            return CompleteCharacterQuestStatusEnum.InventoryChanged;
        }

        foreach (var requirement in requirements.Where(x => x.Type == QuestTypeEnum.Collect))
        {
            if (!inventory.Remove(requirement.ItemType, requirement.Count))
            {
                return CompleteCharacterQuestStatusEnum.InventoryChanged;
            }
        }

        foreach (var reward in rewards)
        {
            if (!inventory.Add(reward.Type, reward.Count, capacity))
            {
                return CompleteCharacterQuestStatusEnum.InventoryFull;
            }
        }

        characterQuest.SynchronizeInventory(characterInventory.Inventory);
        characterInventory.Inventory = inventory;

        var activeCollectQuests = await _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.CharacterId == selectedCharacterId)
            .Where(x => x.Id != characterQuest.Id)
            .Where(x => x.Status == CharacterQuestStatusEnum.Accepted || x.Status == CharacterQuestStatusEnum.Finished)
            .ToArrayAsync(cancellationToken);

        foreach (var activeQuest in activeCollectQuests)
        {
            activeQuest.SynchronizeInventory(inventory);

            collectProgress.Add(new CheckCharacterQuestProgressDto
            {
                QuestId = activeQuest.QuestId,
                CharacterQuestId = activeQuest.Id,
                Progress = activeQuest.Progress,
                ObjectiveProgress = activeQuest.ObjectiveProgress,
                Status = activeQuest.Status
            });
        }

        return CompleteCharacterQuestStatusEnum.Applied;
    }
}
