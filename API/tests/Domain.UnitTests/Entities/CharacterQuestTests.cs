using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.UnitTests.Entities;

public class CharacterQuestTests
{
    [Theory]
    [InlineData(1, CharacterQuestStatusEnum.Accepted)]
    [InlineData(2, CharacterQuestStatusEnum.Finished)]
    [InlineData(3, CharacterQuestStatusEnum.Finished)]
    public void AddProgress_AccumulatesProgressAndFinishesAtRequirement(int progress, CharacterQuestStatusEnum expectedStatus)
    {
        var quest = CreateQuest(CharacterQuestStatusEnum.Accepted);

        quest.AddProgress(progress, requiredProgress: 2);

        Assert.Equal(progress, quest.Progress);
        Assert.Equal(expectedStatus, quest.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddProgress_RejectsNonPositiveAmountWithoutChangingQuest(int progress)
    {
        var quest = CreateQuest(CharacterQuestStatusEnum.Accepted);

        Assert.Throws<ArgumentOutOfRangeException>(() => quest.AddProgress(progress, requiredProgress: 2));

        Assert.Equal(0, quest.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Accepted, quest.Status);
    }

    [Fact]
    public void AddProgress_RejectsQuestThatIsNotAccepted()
    {
        var quest = CreateQuest(CharacterQuestStatusEnum.Finished);

        Assert.Throws<InvalidOperationException>(() => quest.AddProgress(1, requiredProgress: 2));

        Assert.Equal(0, quest.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Finished, quest.Status);
    }

    [Theory]
    [InlineData(0, CharacterQuestStatusEnum.Accepted)]
    [InlineData(1, CharacterQuestStatusEnum.Accepted)]
    [InlineData(2, CharacterQuestStatusEnum.Finished)]
    [InlineData(3, CharacterQuestStatusEnum.Finished)]
    public void SetProgress_SynchronizesCurrentProgressAndStatus(int progress, CharacterQuestStatusEnum expectedStatus)
    {
        var quest = CreateQuest(CharacterQuestStatusEnum.Accepted);

        quest.SetProgress(progress, requiredProgress: 2);

        Assert.Equal(progress, quest.Progress);
        Assert.Equal(expectedStatus, quest.Status);
    }

    [Fact]
    public void SetProgress_ReopensFinishedQuestWhenProgressFallsBelowRequirement()
    {
        var quest = CreateQuest(CharacterQuestStatusEnum.Finished);
        quest.Progress = 2;

        quest.SetProgress(1, requiredProgress: 2);

        Assert.Equal(1, quest.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Accepted, quest.Status);
    }

    [Fact]
    public void Complete_SetsCompletionStateAndTimestamp()
    {
        var completedAtUtc = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
        var quest = CreateQuest(CharacterQuestStatusEnum.Finished);

        quest.Complete(completedAtUtc);

        Assert.Equal(CharacterQuestStatusEnum.Completed, quest.Status);
        Assert.Equal(completedAtUtc, quest.EndDate);
    }

    [Fact]
    public void Complete_RejectsQuestThatIsNotFinished()
    {
        var quest = CreateQuest(CharacterQuestStatusEnum.Accepted);

        Assert.Throws<InvalidOperationException>(() => quest.Complete(DateTimeOffset.UtcNow));

        Assert.Equal(CharacterQuestStatusEnum.Accepted, quest.Status);
        Assert.Equal(default, quest.EndDate);
    }

    private static CharacterQuest CreateQuest(CharacterQuestStatusEnum status)
    {
        return new CharacterQuest
        {
            QuestId = QuestEnum.Kill2Beans,
            CharacterId = 1,
            Status = status
        };
    }
}
