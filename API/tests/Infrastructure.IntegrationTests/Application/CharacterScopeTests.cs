using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Characters.Commands;
using ProjectX.Application.Characters.Queries.GetCharacter;
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
    private const int CurrentCharacterId = 1;
    private const int ForeignCharacterId = 2;

    [Fact]
    public async Task CharacterHandlers_DoNotReadOrModifyAnotherUsersCharacter()
    {
        await using var context = CreateContext();
        context.Characters.AddRange(
            CreateCharacter(CurrentCharacterId, CurrentUserId),
            CreateCharacter(ForeignCharacterId, "other-user"));
        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetCharacterQueryHandler(context, currentUser)
                .Handle(new GetCharacterQuery(ForeignCharacterId), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetCharacterInventoryQueryHandler(context, currentUser)
                .Handle(new GetCharacterInventoryQuery(ForeignCharacterId), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateCharacterCommandHandler(context, currentUser)
                .Handle(
                    new UpdateCharacterCommand { CharacterId = ForeignCharacterId, Health = 1 },
                    CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new AddCharacterExperienceCommandHandler(context, currentUser)
                .Handle(
                    new AddCharacterExperienceCommand
                    {
                        CharacterId = ForeignCharacterId,
                        Amount = 100,
                        Type = ExperienceTypeEnum.Cooking
                    },
                    CancellationToken.None));

        var currentCharacter = await new GetCharacterQueryHandler(context, currentUser)
            .Handle(new GetCharacterQuery(CurrentCharacterId), CancellationToken.None);

        Assert.Equal("current-user-character", currentCharacter.Name);
        Assert.Equal(100, currentCharacter.Health);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Character CreateCharacter(int id, string userId)
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
                Inventory = new InventoryState([]),
                Count = 15
            }
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

        public DateTimeOffset? GetAuthenticatedSessionStartedAtUtc()
        {
            return null;
        }

        public DateTimeOffset? GetAuthenticatedTokenExpirationUtc()
        {
            return null;
        }
    }
}
