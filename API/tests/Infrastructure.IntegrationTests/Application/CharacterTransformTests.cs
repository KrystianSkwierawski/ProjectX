using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectX.Application.CharacterTransforms.Commands.SaveCharacterTransform;
using ProjectX.Application.CharacterTransforms.Queries.GetCharacterTransform;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Application;

public class CharacterTransformTests
{
    [Fact]
    public async Task ReturnLocation_IsSavedForSessionCharacterAndReadOnlyByOwner()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        context.Characters.AddRange(
            new Character { Id = 42, ApplicationUserId = "owner", Name = "First" },
            new Character { Id = 43, ApplicationUserId = "owner", Name = "Second" });

        await context.SaveChangesAsync();

        var user = new Mock<ICurrentUserService>();
        user.Setup(x => x.GetId()).Returns("owner");
        user.Setup(x => x.GetCharacterId()).Returns(43);

        var write = new SaveCharacterTransformCommandHandler(context, user.Object);
        var read = new GetPlayerPositionQueryHandler(context, user.Object);

        await write.Handle(new SaveCharacterTransformCommand
        {
            SceneName = "EnvironmentScene",
            PositionX = -5,
            PositionY = 1.15f,
            PositionZ = -6,
            RotationY = 90
        }, CancellationToken.None);

        context.ChangeTracker.Clear();

        var saved = await read.Handle(new(43), CancellationToken.None);

        Assert.Equal("EnvironmentScene", saved.SceneName);
        Assert.Equal(-5, saved.PositionX);
        Assert.Equal(1.15f, saved.PositionY);
        Assert.Equal(-6, saved.PositionZ);
        Assert.Equal(90, saved.RotationY);
        Assert.Single(context.CharacterTransforms);
        Assert.Equal(43, saved.CharacterId);

        await Assert.ThrowsAsync<NotFoundException>(() => read.Handle(new(42), CancellationToken.None));

        user.Setup(x => x.GetId()).Returns("foreign");

        await Assert.ThrowsAsync<NotFoundException>(() => read.Handle(new(43), CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => write.Handle(new(), CancellationToken.None));
        Assert.Single(context.CharacterTransforms);
    }
}
