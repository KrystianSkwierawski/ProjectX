using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectX.Application.CharacterSettings.Commands.UpdateCharacterSettings;
using ProjectX.Application.CharacterSettings.Queries.GetCharacterSettings;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Application;

public class CharacterSettingsTests
{
    [Fact]
    public async Task Settings_DefaultAndSavedValuesAreScopedToOwnedCharacter()
    {
        await using var context = CreateContext();

        context.Characters.AddRange(
            new Character { Id = 42, ApplicationUserId = "owner", Name = "First" },
            new Character { Id = 43, ApplicationUserId = "owner", Name = "Second" },
            new Character { Id = 99, ApplicationUserId = "foreign", Name = "Foreign" });

        await context.SaveChangesAsync();

        var user = new Mock<ICurrentUserService>();

        user.Setup(x => x.GetId()).Returns("owner");
        user.SetupGet(x => x.Language).Returns(LanguageEnum.pl);

        var read = new GetCharacterSettingsQueryHandler(context, user.Object);
        var write = new UpdateCharacterSettingsCommandHandler(context, user.Object);

        var defaults = await read.Handle(new(42), CancellationToken.None);

        Assert.Equal(LanguageEnum.pl, defaults.Language);
        Assert.Equal(new InventoryItemEnum[10], defaults.ActionBars);
        Assert.Empty(context.CharacterSettings);

        var slots = new InventoryItemEnum[10];

        slots[0] = InventoryItemEnum.HealthPotion;
        slots[9] = InventoryItemEnum.IronSword;

        await write.Handle(new(42, LanguageEnum.en, slots), CancellationToken.None);

        context.ChangeTracker.Clear();

        var saved = await read.Handle(new(42), CancellationToken.None);

        Assert.Equal(LanguageEnum.en, saved.Language);
        Assert.Equal(slots, saved.ActionBars);
        Assert.Equal(new InventoryItemEnum[10], (await read.Handle(new(43), CancellationToken.None)).ActionBars);

        slots[0] = InventoryItemEnum.None;

        await write.Handle(new(42, LanguageEnum.en, slots), CancellationToken.None);

        Assert.Single(context.CharacterSettings);
        Assert.Equal(slots, (await read.Handle(new(42), CancellationToken.None)).ActionBars);

        await Assert.ThrowsAsync<NotFoundException>(() => read.Handle(new(99), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => write.Handle(new(99, LanguageEnum.en, slots), CancellationToken.None));

        Assert.False(await context.CharacterSettings.AnyAsync(x => x.CharacterId == 99));
    }

    [Fact]
    public void SettingsConverter_PreservesEmptyPositionsDuplicatesAndSnapshotIsolation()
    {
        using var context = CreateContext();

        var property = context.Model.FindEntityType(typeof(CharacterSettings))!.FindProperty(nameof(CharacterSettings.ActionBars))!;

        var converter = property.GetValueConverter()!;
        var comparer = property.GetValueComparer()!;

        var slots = new InventoryItemEnum[10];

        slots[0] = slots[9] = InventoryItemEnum.HealthPotion;

        var copy = Assert.IsType<InventoryItemEnum[]>(converter.ConvertFromProvider(converter.ConvertToProvider(slots)));
        var snapshot = comparer.Snapshot(slots);

        Assert.Equal(slots, copy);

        slots[0] = InventoryItemEnum.None;

        Assert.False(comparer.Equals(slots, snapshot));
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
