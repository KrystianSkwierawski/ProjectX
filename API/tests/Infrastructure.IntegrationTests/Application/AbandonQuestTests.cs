using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectX.Application.CharacterQuests.Commands.AbandonCharacterQuest;
using ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;
using ProjectX.Application.CharacterQuests.Commands.CheckCharacterQuestProgress;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Application;

public class AbandonQuestTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AbandonActiveQuest_ResetsKillsForReaccept_AndPreservesInventory(bool finished)
    {
        await using var context = await CreateContext();
        var user = CurrentUser();
        var accepted = await new AcceptCharacterQuestCommandHandler(context, user, TimeProvider.System)
            .Handle(new(QuestEnum.FieldSupplies), default);

        await new CheckCharacterQuestProgressCommandHandler(context, user)
            .Handle(new(QuestEnum.FieldSupplies, finished ? 2 : 1, "kill:Bean(Clone)"), default);

        context.ChangeTracker.Clear();

        var questBeforeAbandonment = await context.CharacterQuests.SingleAsync();

        Assert.Equal(
            finished ? CharacterQuestStatusEnum.Finished : CharacterQuestStatusEnum.Accepted,
            questBeforeAbandonment.Status);

        var result = await Handler(context).Handle(new(accepted.Id), default);

        context.ChangeTracker.Clear();

        Assert.True(result.Abandoned);
        Assert.Empty(await context.CharacterQuests.ToArrayAsync());
        Assert.Empty(await context.CharacterExperiences.ToArrayAsync());

        var inventory = await context.CharacterInventories.SingleAsync();

        Assert.Equal(2, inventory.Inventory.GetCount(InventoryItemEnum.Can));
        Assert.Equal(1, inventory.Inventory.GetCount(InventoryItemEnum.Fish));

        var reaccepted = await new AcceptCharacterQuestCommandHandler(context, user, TimeProvider.System)
            .Handle(new(QuestEnum.FieldSupplies), default);

        Assert.NotEqual(accepted.Id, reaccepted.Id);
        Assert.Equal(CharacterQuestStatusEnum.Accepted, reaccepted.Status);
        Assert.Equal(0, reaccepted.ObjectiveProgress.GetValueOrDefault("kill:Bean(Clone)"));
        Assert.Equal(2, reaccepted.ObjectiveProgress["item:100"]);
        Assert.Equal(1, reaccepted.ObjectiveProgress["item:200"]);

        var replay = await Handler(context).Handle(new(accepted.Id), default);

        Assert.True(replay.Abandoned);
        Assert.Equal(reaccepted.Id, (await context.CharacterQuests.SingleAsync()).Id);
    }

    [Fact]
    public async Task AbandonCompletedOrForeignQuest_DoesNotRemoveHistoryOrRewards()
    {
        await using var context = await CreateContext();
        var completed = new CharacterQuest
        {
            CharacterId = 41,
            QuestId = QuestEnum.FieldSupplies,
            Status = CharacterQuestStatusEnum.Completed
        };
        var foreign = new CharacterQuest
        {
            CharacterId = 42,
            QuestId = QuestEnum.FieldSupplies,
            Status = CharacterQuestStatusEnum.Accepted
        };
        context.AddRange(completed, foreign);

        await context.SaveChangesAsync();

        var completedResult = await Handler(context).Handle(new(completed.Id), default);
        var foreignResult = await Handler(context).Handle(new(foreign.Id), default);
        context.ChangeTracker.Clear();

        Assert.False(completedResult.Abandoned);
        Assert.True(foreignResult.Abandoned); // Scoped no-op does not disclose another character's row.
        Assert.Equal(2, await context.CharacterQuests.CountAsync());
        Assert.Equal(CharacterQuestStatusEnum.Completed, (await context.CharacterQuests
            .Where(quest => quest.Id == completed.Id)
            .SingleAsync()).Status);
    }

    [Fact]
    public async Task ConcurrentCompletion_PreventsAbandonmentOfRewardedQuest()
    {
        var options = Options();
        await using var staleContext = await CreateContext(options);
        var accepted = await new AcceptCharacterQuestCommandHandler(staleContext, CurrentUser(), TimeProvider.System)
            .Handle(new(QuestEnum.FieldSupplies), default);

        await using var completingContext = new ApplicationDbContext(options);
        var completed = await completingContext.CharacterQuests.SingleAsync();
        completed.Status = CharacterQuestStatusEnum.Completed;

        await completingContext.SaveChangesAsync();

        var result = await Handler(staleContext).Handle(new(accepted.Id), default);

        Assert.False(result.Abandoned);
        completingContext.ChangeTracker.Clear();
        Assert.Equal(CharacterQuestStatusEnum.Completed, (await completingContext.CharacterQuests.SingleAsync()).Status);
    }

    private static AbandonCharacterQuestCommandHandler Handler(ApplicationDbContext context)
    {
        return new(context, CurrentUser(), NullLogger<AbandonCharacterQuestCommandHandler>.Instance);
    }

    private static ICurrentUserService CurrentUser()
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(service => service.GetId()).Returns("owner");
        user.Setup(service => service.GetCharacterId()).Returns(41);

        return user.Object;
    }

    private static DbContextOptions<ApplicationDbContext> Options()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static async Task<ApplicationDbContext> CreateContext(DbContextOptions<ApplicationDbContext>? options = null)
    {
        var context = new ApplicationDbContext(options ?? Options());
        var character = new Character
        {
            Id = 41,
            Name = "Owner",
            ApplicationUserId = "owner",
            Status = StatusEnum.Active,
            CharacterInventory = new CharacterInventory
            {
                Id = 41,
                Count = 15,
                Inventory = new InventoryState([new InventorySlot(InventoryItemEnum.Can, 2), new InventorySlot(InventoryItemEnum.Fish, 1)])
            }
        };

        context.AddRange(character, new Quest { Id = QuestEnum.FieldSupplies, Name = "FieldSupplies", Status = StatusEnum.Active });

        await context.SaveChangesAsync();

        return context;
    }
}
