using ProjectX.Domain.Common;
using ProjectX.Domain.Inventory;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class CharacterQuest : BaseAuditableEntity
{
    public int Id { get; set; }

    public QuestEnum QuestId { get; set; }

    public int CharacterId { get; set; }

    public CharacterQuestStatusEnum Status { get; set; }

    public int Progress { get; set; }

    public Dictionary<string, int> ObjectiveProgress { get; set; } = [];

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset EndDate { get; set; }

    public virtual Character Character { get; set; } = null!;

    public virtual Quest Quest { get; set; } = null!;

    public void AddKillProgress(string requirementId, int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        EnsureActive();

        var requirement = QuestId.GetRequirements()
            .Where(x => x.Id == requirementId)
            .Where(x => x.Type == QuestTypeEnum.Kill)
            .Single();

        ObjectiveProgress[requirement.Id] = checked(ObjectiveProgress.GetValueOrDefault(requirement.Id) + amount);
        RefreshStatus();
    }

    public void SynchronizeInventory(InventoryState inventory)
    {
        EnsureActive();

        foreach (var requirement in QuestId.GetRequirements().Where(x => x.Type == QuestTypeEnum.Collect))
        {
            ObjectiveProgress[requirement.Id] = inventory.GetCount(requirement.ItemType);
        }

        RefreshStatus();
    }

    public bool RequirementsMet => QuestId.GetRequirements()
        .All(x => ObjectiveProgress.GetValueOrDefault(x.Id) >= x.Count);

    private void RefreshStatus()
    {
        Progress = ObjectiveProgress.Values.Sum();
        Status = RequirementsMet ? CharacterQuestStatusEnum.Finished : CharacterQuestStatusEnum.Accepted;
    }

    private void EnsureActive()
    {
        if (Status is not CharacterQuestStatusEnum.Accepted and not CharacterQuestStatusEnum.Finished)
        {
            throw new InvalidOperationException("Only an active quest can receive progress.");
        }
    }

    public void Complete(DateTimeOffset completedAtUtc)
    {
        if (Status != CharacterQuestStatusEnum.Finished || !RequirementsMet)
        {
            throw new InvalidOperationException("Only a finished quest can be completed.");
        }

        EndDate = completedAtUtc;
        Status = CharacterQuestStatusEnum.Completed;
    }
}
