using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
using ProjectX.Application.CharacterInventories.Commands.UpdateCharacterInventory;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;
using ProjectX.Application.CharacterQuests.Commands.CheckCharacterQuestProgress;
using ProjectX.Application.CharacterQuests.Commands.CompleteCharacterQuest;
using ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;
using ProjectX.Application.Characters.Commands;
using ProjectX.Application.Characters.Queries.GetCharacter;
using ProjectX.Application.Characters.Queries.GetCharacters;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Inventory;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Application;

public class CharacterScopeTests
{
    private const string CurrentUserId = "current-user";
    private const int CurrentCharacterId = 42;
    private const int OtherOwnedCharacterId = 43;
    private const int ForeignCharacterId = 1;

    [Fact]
    public async Task ClientQueries_DoNotReadAnotherUsersCharacterData()
    {
        await using var context = CreateContext();

        context.Characters.AddRange(
            CreateCharacter(CurrentCharacterId, CurrentUserId),
            CreateCharacter(ForeignCharacterId, "other-user"));

        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetCharacterInventoryQueryHandler(context, currentUser)
                .Handle(new GetCharacterInventoryQuery(ForeignCharacterId), CancellationToken.None));

        var currentCharacter = await new GetCharacterQueryHandler(context, currentUser)
            .Handle(new GetCharacterQuery(), CancellationToken.None);

        Assert.Equal(CurrentCharacterId, currentCharacter.Id);
        Assert.Equal("current-user-character", currentCharacter.Name);
        Assert.Equal(100, currentCharacter.Health);
    }

    [Fact]
    public async Task CharacterHandlers_ReadAndModifyOwnedCharacter()
    {
        await using var context = CreateContext();

        context.Characters.AddRange(
            CreateCharacter(CurrentCharacterId, CurrentUserId),
            CreateCharacter(ForeignCharacterId, "other-user"));

        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();

        await new UpdateCharacterCommandHandler(context, currentUser)
            .Handle(
                new UpdateCharacterCommand
                {
                    Health = 75,
                    Strength = 9
                },
                CancellationToken.None);

        var experience = await new AddCharacterExperienceCommandHandler(context, currentUser)
            .Handle(
                new AddCharacterExperienceCommand
                {
                    Amount = 100,
                    Type = ExperienceTypeEnum.Cooking
                },
                CancellationToken.None);

        var character = await new GetCharacterQueryHandler(context, currentUser)
            .Handle(new GetCharacterQuery(), CancellationToken.None);

        Assert.Equal(CurrentCharacterId, character.Id);
        Assert.Equal(75, character.Health);
        Assert.Equal(9, character.Strength);
        Assert.Equal(100, experience.Experience);
        Assert.Equal(2, experience.Level);
        Assert.Equal(2, character.Levels[ExperienceTypeEnum.Cooking]);

        var foreignCharacter = await context.Characters
            .Include(x => x.CharacterExperiences)
            .Where(x => x.Id == ForeignCharacterId)
            .SingleAsync();

        Assert.Equal(100, foreignCharacter.Health);
        Assert.Empty(foreignCharacter.CharacterExperiences);
    }

    [Fact]
    public async Task InventoryHandlers_ReadAndModifyOwnedInventory()
    {
        await using var context = CreateContext();

        context.Characters.AddRange(
            CreateCharacter(
                CurrentCharacterId,
                CurrentUserId,
                new InventorySlot(InventoryItemEnum.HealthPotion, 2),
                new InventorySlot(InventoryItemEnum.Currency, 10)),
            CreateCharacter(
                ForeignCharacterId,
                "other-user",
                new InventorySlot(InventoryItemEnum.Fish, 7)));

        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();

        await new UpdateCharacterInventoryCommandHandler(context, currentUser)
            .Handle(
                new UpdateCharacterInventoryCommand(
                    [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 3 }],
                    [new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 4 }]),
                CancellationToken.None);

        context.ChangeTracker.Clear();

        var inventory = await new GetCharacterInventoryQueryHandler(context, currentUser)
            .Handle(new GetCharacterInventoryQuery(CurrentCharacterId), CancellationToken.None);

        var foreignInventory = await context.CharacterInventories
            .Where(x => x.Id == ForeignCharacterId)
            .SingleAsync();

        Assert.Collection(
            inventory.Inventory.Items,
            x => Assert.Equal((InventoryItemEnum.HealthPotion, 5), (x.Type, x.Count)),
            x => Assert.Equal((InventoryItemEnum.Currency, 6), (x.Type, x.Count)));
        Assert.Equal((InventoryItemEnum.Fish, 7), (foreignInventory.Inventory.Items.Single().Type, foreignInventory.Inventory.Items.Single().Count));
    }

    [Fact]
    public async Task QuestHandlers_ReadAndModifyOnlyOwnedQuests()
    {
        await using var context = CreateContext();

        var currentCharacter = CreateCharacter(CurrentCharacterId, CurrentUserId);
        var foreignCharacter = CreateCharacter(ForeignCharacterId, "other-user");

        var quest = new Quest
        {
            Id = QuestEnum.Kill2Beans,
            Name = nameof(QuestEnum.Kill2Beans),
            Type = QuestTypeEnum.Kill,
            GameObjectName = "Bean(Clone)",
            Requirement = 2,
            Reward = 1000,
            Status = StatusEnum.Active
        };

        var currentQuest = CreateCharacterQuest(10, currentCharacter, quest, CharacterQuestStatusEnum.Accepted);
        var foreignAcceptedQuest = CreateCharacterQuest(20, foreignCharacter, quest, CharacterQuestStatusEnum.Accepted);
        var foreignFinishedQuest = CreateCharacterQuest(21, foreignCharacter, quest, CharacterQuestStatusEnum.Finished);

        context.AddRange(currentCharacter, foreignCharacter, quest, currentQuest, foreignAcceptedQuest, foreignFinishedQuest);

        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();

        var initialQuests = await new GetCharacterQuestsHandler(context, currentUser)
            .Handle(new GetCharacterQuestsQuery(CurrentCharacterId), CancellationToken.None);

        var progress = await new CheckCharacterQuestProgressCommandHandler(context, currentUser)
            .Handle(
                new CheckCharacterQuestProgressCommand(QuestEnum.Kill2Beans, 2),
                CancellationToken.None);

        var completedAtUtc = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

        var completion = await new CompleteCharacterQuestCommandHandler(context, currentUser, new FixedTimeProvider(completedAtUtc))
            .Handle(new CompleteCharacterQuestCommand(currentQuest.Id), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new CompleteCharacterQuestCommandHandler(context, currentUser, new FixedTimeProvider(completedAtUtc))
                .Handle(new CompleteCharacterQuestCommand(foreignFinishedQuest.Id), CancellationToken.None));

        Assert.Collection(initialQuests.CharacterQuests, x => Assert.Equal(currentQuest.Id, x.Id));
        Assert.Equal(CharacterQuestStatusEnum.Finished, progress.Status);
        Assert.Equal(1000, completion.Reward);
        Assert.Equal(CharacterQuestStatusEnum.Completed, currentQuest.Status);
        Assert.Equal(completedAtUtc, currentQuest.EndDate);
        Assert.Equal(CharacterQuestStatusEnum.Accepted, foreignAcceptedQuest.Status);
        Assert.Equal(0, foreignAcceptedQuest.Progress);
        Assert.Equal(CharacterQuestStatusEnum.Finished, foreignFinishedQuest.Status);
    }

    [Fact]
    public async Task CompleteCollectQuest_RemovesRequiredItemsExactlyOnce()
    {
        await using var context = CreateContext();

        var character = CreateCharacter(
            CurrentCharacterId,
            CurrentUserId,
            new InventorySlot(InventoryItemEnum.Can, 3));

        var quest = new Quest
        {
            Id = QuestEnum.Collect2Cans,
            Name = nameof(QuestEnum.Collect2Cans),
            Type = QuestTypeEnum.Collect,
            GameObjectName = nameof(InventoryItemEnum.Can),
            Requirement = 2,
            Reward = 1000,
            Status = StatusEnum.Active
        };

        var characterQuest = CreateCharacterQuest(
            30,
            character,
            quest,
            CharacterQuestStatusEnum.Finished);

        context.AddRange(character, quest, characterQuest);

        await context.SaveChangesAsync();

        var completedAtUtc = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

        var result = await new CompleteCharacterQuestCommandHandler(
                context,
                new TestCurrentUserService(),
                new FixedTimeProvider(completedAtUtc))
            .Handle(new CompleteCharacterQuestCommand(characterQuest.Id), CancellationToken.None);

        var remainingItem = Assert.Single(character.CharacterInventory.Inventory.Items);

        Assert.Equal(1000, result.Reward);
        Assert.Equal(CharacterQuestStatusEnum.Completed, characterQuest.Status);
        Assert.Equal((InventoryItemEnum.Can, 1), (remainingItem.Type, remainingItem.Count));
    }

    [Fact]
    public async Task GetCharacters_ReturnsAllActiveCharactersOwnedByUser()
    {
        await using var context = CreateContext();

        var removedCharacter = CreateCharacter(44, CurrentUserId);
        removedCharacter.Status = StatusEnum.Removed;

        context.Characters.AddRange(
            CreateCharacter(CurrentCharacterId, CurrentUserId),
            CreateCharacter(OtherOwnedCharacterId, CurrentUserId),
            removedCharacter,
            CreateCharacter(ForeignCharacterId, "other-user"));

        await context.SaveChangesAsync();

        var result = await new GetCharactersQueryHandler(context, new TestCurrentUserService())
            .Handle(new GetCharactersQuery(), CancellationToken.None);

        Assert.Collection(
            result.Characters,
            x => Assert.Equal(CurrentCharacterId, x.Id),
            x => Assert.Equal(OtherOwnedCharacterId, x.Id));
    }

    [Fact]
    public async Task ServerHandlers_UseCharacterSelectedByPlayerSession()
    {
        await using var context = CreateContext();

        var currentCharacter = CreateCharacter(CurrentCharacterId, CurrentUserId);
        var otherOwnedCharacter = CreateCharacter(OtherOwnedCharacterId, CurrentUserId);
        var quest = new Quest
        {
            Id = QuestEnum.Kill2Beans,
            Name = nameof(QuestEnum.Kill2Beans),
            Type = QuestTypeEnum.Kill,
            GameObjectName = "Bean(Clone)",
            Requirement = 2,
            Reward = 1000,
            Status = StatusEnum.Active
        };

        context.AddRange(currentCharacter, otherOwnedCharacter, quest);

        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();

        await new UpdateCharacterCommandHandler(context, currentUser)
            .Handle(
                new UpdateCharacterCommand { Health = 75 },
                CancellationToken.None);

        await new UpdateCharacterInventoryCommandHandler(context, currentUser)
            .Handle(
                new UpdateCharacterInventoryCommand(
                    [new InventoryItemDto { Type = InventoryItemEnum.HealthPotion, Count = 1 }],
                    []),
                CancellationToken.None);

        var acceptedQuest = await new AcceptCharacterQuestCommandHandler(
                context,
                currentUser,
                new FixedTimeProvider(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero)))
            .Handle(
                new AcceptCharacterQuestCommand(QuestEnum.Kill2Beans),
                CancellationToken.None);

        var selectedCharacter = await new GetCharacterQueryHandler(context, currentUser)
            .Handle(new GetCharacterQuery(), CancellationToken.None);

        var persistedQuest = await context.CharacterQuests
            .Where(x => x.Id == acceptedQuest.Id)
            .SingleAsync();

        Assert.Equal(CurrentCharacterId, selectedCharacter.Id);
        Assert.Equal(75, currentCharacter.Health);
        Assert.Equal(100, otherOwnedCharacter.Health);
        Assert.Single(currentCharacter.CharacterInventory.Inventory.Items);
        Assert.Empty(otherOwnedCharacter.CharacterInventory.Inventory.Items);
        Assert.Equal(CurrentCharacterId, persistedQuest.CharacterId);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Character CreateCharacter(int id, string userId, params InventorySlot[] inventory)
    {
        return new Character
        {
            Id = id,
            ApplicationUserId = userId,
            Name = $"{userId}-character",
            Health = 100,
            MaxHealth = 100,
            Status = StatusEnum.Active,
            CharacterInventory = new CharacterInventory
            {
                Id = id,
                Inventory = new InventoryState(inventory),
                Count = 15
            }
        };
    }

    private static CharacterQuest CreateCharacterQuest(
        int id,
        Character character,
        Quest quest,
        CharacterQuestStatusEnum status)
    {
        return new CharacterQuest
        {
            Id = id,
            CharacterId = character.Id,
            Character = character,
            QuestId = quest.Id,
            Quest = quest,
            Status = status,
            StartDate = new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero)
        };
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public LanguageEnum Language => LanguageEnum.en;

        public List<string>? Roles => [];

        public string GetId()
        {
            return CurrentUserId;
        }

        public string GetAuthenticatedUserId()
        {
            return CurrentUserId;
        }

        public int? GetCharacterId()
        {
            return CurrentCharacterId;
        }

        public DateTimeOffset? GetAuthenticatedSessionStartedAtUtc()
        {
            return null;
        }

        public DateTimeOffset? GetAuthenticatedTokenExpirationUtc()
        {
            return null;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
