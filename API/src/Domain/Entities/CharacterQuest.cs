using ProjectX.Domain.Common;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class CharacterQuest : BaseAuditableEntity
{
    public int Id { get; set; }

    public QuestEnum QuestId { get; set; }

    public int CharacterId { get; set; }

    public CharacterQuestStatusEnum Status { get; set; }

    public int Progress { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset EndDate { get; set; }

    public virtual Character Character { get; set; } = null!;

    public virtual Quest Quest { get; set; } = null!;

    public void AddProgress(int amount, int requiredProgress)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (Status != CharacterQuestStatusEnum.Accepted)
        {
            throw new InvalidOperationException("Only an accepted quest can receive progress.");
        }

        Progress += amount;

        if (Progress >= requiredProgress)
        {
            Status = CharacterQuestStatusEnum.Finished;
        }
    }

    public void SetProgress(int progress, int requiredProgress)
    {
        if (progress < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(progress));
        }

        if (Status is not CharacterQuestStatusEnum.Accepted and not CharacterQuestStatusEnum.Finished)
        {
            throw new InvalidOperationException("Only an active quest can synchronize progress.");
        }

        Progress = progress;
        Status = progress >= requiredProgress
            ? CharacterQuestStatusEnum.Finished
            : CharacterQuestStatusEnum.Accepted;
    }

    public void Complete(DateTimeOffset completedAtUtc)
    {
        if (Status != CharacterQuestStatusEnum.Finished)
        {
            throw new InvalidOperationException("Only a finished quest can be completed.");
        }

        EndDate = completedAtUtc;
        Status = CharacterQuestStatusEnum.Completed;
    }
}
