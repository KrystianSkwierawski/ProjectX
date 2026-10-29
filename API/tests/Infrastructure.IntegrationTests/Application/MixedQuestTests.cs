using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectX.Application.CharacterInventories.Commands.UpdateCharacterInventory;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;
using ProjectX.Application.CharacterQuests.Commands.CheckCharacterQuestProgress;
using ProjectX.Application.CharacterQuests.Commands.CompleteCharacterQuest;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Application;

public class MixedQuestTests
{
    [Fact]
    public async Task MixedGoals_SurviveReloadAndItemLoss_AndCompleteAtomically()
    {
        await using var context = await CreateContext();
        var user = CurrentUser();
        var accepted = await new AcceptCharacterQuestCommandHandler(context, user, TimeProvider.System)
            .Handle(new(QuestEnum.FieldSupplies), default);

        Assert.Equal(CharacterQuestStatusEnum.Accepted, accepted.Status);
        Assert.Equal(2, accepted.ObjectiveProgress["item:100"]);
        Assert.Equal(1, accepted.ObjectiveProgress["item:200"]);

        var check = new CheckCharacterQuestProgressCommandHandler(context, user);
        var killed = await check.Handle(new(QuestEnum.FieldSupplies, 2, "kill:Bean(Clone)"), default);

        Assert.Equal(CharacterQuestStatusEnum.Finished, killed.Status);

        var inventory = new UpdateCharacterInventoryCommandHandler(context, user);
        await inventory.Handle(new([], [new InventoryItemDto { Type = InventoryItemEnum.Can, Count = 1 }]), default);
        context.ChangeTracker.Clear();

        var stored = await context.CharacterQuests.SingleAsync();

        Assert.Equal(CharacterQuestStatusEnum.Accepted, stored.Status);
        Assert.Equal(2, stored.ObjectiveProgress["kill:Bean(Clone)"]);
        Assert.Equal(1, stored.ObjectiveProgress["item:100"]);

        await inventory.Handle(new([new InventoryItemDto { Type = InventoryItemEnum.Can, Count = 1 }], []), default);
        var result = await new CompleteCharacterQuestCommandHandler(context, user, TimeProvider.System)
            .Handle(new(accepted.Id), default);
        context.ChangeTracker.Clear();

        var persisted = await context.CharacterInventories.SingleAsync();

        Assert.Equal(CompleteCharacterQuestStatusEnum.Applied, result.Status);
        Assert.Equal(0, persisted.Inventory.GetCount(InventoryItemEnum.Can));
        Assert.Equal(0, persisted.Inventory.GetCount(InventoryItemEnum.Fish));
        Assert.Equal(40, persisted.Inventory.GetCount(InventoryItemEnum.Currency));
        Assert.Equal(3, persisted.Inventory.GetCount(InventoryItemEnum.HealthPotion));
        Assert.Equal(1500, result.Reward);
        Assert.Equal(CharacterQuestStatusEnum.Completed, (await context.CharacterQuests.SingleAsync()).Status);
    }

    [Fact]
    public async Task MissingKillsOrUnknownObjective_CannotGrantRewards()
    {
        await using var context = await CreateContext();
        var user = CurrentUser();
        var accepted = await new AcceptCharacterQuestCommandHandler(context, user, TimeProvider.System)
            .Handle(new(QuestEnum.FieldSupplies), default);
        var quest = await context.CharacterQuests.SingleAsync();
        quest.Status = CharacterQuestStatusEnum.Finished;
        quest.ObjectiveProgress["kill:Bean(Clone)"] = 1;

        await context.SaveChangesAsync();

        var result = await new CompleteCharacterQuestCommandHandler(context, user, TimeProvider.System)
            .Handle(new(accepted.Id), default);

        Assert.Equal(CompleteCharacterQuestStatusEnum.InventoryChanged, result.Status);
        Assert.Empty(context.CharacterExperiences);
        Assert.Equal(2, (await context.CharacterInventories.SingleAsync()).Inventory.GetCount(InventoryItemEnum.Can));

        await Assert.ThrowsAsync<InvalidOperationException>(() => new CheckCharacterQuestProgressCommandHandler(context, user)
            .Handle(new(QuestEnum.FieldSupplies, 100, "not-a-target"), default));

        Assert.Equal(1, quest.ObjectiveProgress["kill:Bean(Clone)"]);
    }

    private static ICurrentUserService CurrentUser()
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(x => x.GetId()).Returns("owner");
        user.Setup(x => x.GetCharacterId()).Returns(41);

        return user.Object;
    }

    private static async Task<ApplicationDbContext> CreateContext()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
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
