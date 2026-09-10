using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Friends;
using ProjectX.Application.Friends.Commands.RemoveFriend;
using ProjectX.Application.Friends.Commands.RespondFriendInvitation;
using ProjectX.Application.Friends.Commands.SendFriendInvitation;
using ProjectX.Application.Friends.Queries.AuthorizeWhisper;
using ProjectX.Application.Friends.Queries.GetFriendList;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Infrastructure.Persistance;

namespace ProjectX.Infrastructure.IntegrationTests.Application;

public class FriendScopeTests
{
    private const int RequesterId = 42;
    private const int RecipientId = 7;
    private const int UnrelatedCharacterId = 13;

    [Fact]
    public async Task Invitation_AppearsOnTheCorrectSideForEachCharacter()
    {
        await using var context = CreateContext();
        context.Characters.AddRange(
            CreateCharacter(RequesterId, "requester", "Requester"),
            CreateCharacter(RecipientId, "recipient", "Recipient"));
        await context.SaveChangesAsync();

        var result = await new SendFriendInvitationCommandHandler(context, new TestCurrentUserService(RequesterId))
            .Handle(new SendFriendInvitationCommand { CharacterName = "Recipient" }, CancellationToken.None);

        var sessions = new Mock<IGameSessionService>();
        var requesterList = await new GetFriendListQueryHandler(context, new TestCurrentUserService(RequesterId), sessions.Object)
            .Handle(new GetFriendListQuery(), CancellationToken.None);
        var recipientList = await new GetFriendListQueryHandler(context, new TestCurrentUserService(RecipientId), sessions.Object)
            .Handle(new GetFriendListQuery(), CancellationToken.None);

        Assert.Equal(FriendOperationStatusEnum.Applied, result.Status);
        Assert.Empty(requesterList.Friends);
        Assert.Collection(requesterList.OutgoingInvitations, x => Assert.Equal(RecipientId, x.CharacterId));
        Assert.Collection(recipientList.IncomingInvitations, x => Assert.Equal(RequesterId, x.CharacterId));
    }

    [Fact]
    public async Task AcceptedInvitation_AllowsWhispersAndExposesOnlineState()
    {
        await using var context = CreateContext();
        context.Characters.AddRange(
            CreateCharacter(RequesterId, "requester", "Requester"),
            CreateCharacter(RecipientId, "recipient", "Recipient"));
        context.CharacterFriendships.Add(CharacterFriendship.Create(RequesterId, RecipientId));
        context.CharacterExperiences.Add(new CharacterExperience
        {
            CharacterId = RecipientId,
            Amount = 400,
            Type = ExperienceTypeEnum.Main
        });
        await context.SaveChangesAsync();

        var response = await new RespondFriendInvitationCommandHandler(context, new TestCurrentUserService(RecipientId))
            .Handle(new RespondFriendInvitationCommand { CharacterId = RequesterId, Accept = true }, CancellationToken.None);

        var sessions = new Mock<IGameSessionService>();
        sessions.Setup(x => x.IsCharacterOnline($"user-{RequesterId}", RecipientId)).Returns(true);

        var friendList = await new GetFriendListQueryHandler(context, new TestCurrentUserService(RequesterId), sessions.Object)
            .Handle(new GetFriendListQuery(), CancellationToken.None);
        var whisper = await new AuthorizeWhisperQueryHandler(context, new TestCurrentUserService(RequesterId))
            .Handle(new AuthorizeWhisperQuery(RecipientId), CancellationToken.None);

        Assert.Equal(FriendOperationStatusEnum.Applied, response.Status);
        Assert.Collection(
            friendList.Friends,
            x =>
            {
                Assert.Equal(RecipientId, x.CharacterId);
                Assert.Equal(3, x.Level);
                Assert.True(x.IsOnline);
            });
        Assert.True(whisper.IsAllowed);
        Assert.Equal(FriendOperationStatusEnum.Applied, whisper.Status);
    }

    [Fact]
    public async Task UnrelatedCharacter_CannotAcceptAnotherCharactersInvitation()
    {
        await using var context = CreateContext();
        context.Characters.AddRange(
            CreateCharacter(RequesterId, "requester", "Requester"),
            CreateCharacter(RecipientId, "recipient", "Recipient"),
            CreateCharacter(UnrelatedCharacterId, "unrelated", "Unrelated"));
        context.CharacterFriendships.Add(CharacterFriendship.Create(RequesterId, RecipientId));
        await context.SaveChangesAsync();

        var result = await new RespondFriendInvitationCommandHandler(context, new TestCurrentUserService(UnrelatedCharacterId))
            .Handle(new RespondFriendInvitationCommand { CharacterId = RequesterId, Accept = true }, CancellationToken.None);

        var invitation = await context.CharacterFriendships.SingleAsync();

        Assert.Equal(FriendOperationStatusEnum.InvitationNotFound, result.Status);
        Assert.Equal(FriendshipStatusEnum.Pending, invitation.Status);
    }

    [Fact]
    public async Task RemovingFriend_ImmediatelyRevokesWhisperAuthorization()
    {
        await using var context = CreateContext();
        context.Characters.AddRange(
            CreateCharacter(RequesterId, "requester", "Requester"),
            CreateCharacter(RecipientId, "recipient", "Recipient"));
        var friendship = CharacterFriendship.Create(RequesterId, RecipientId);
        friendship.Accept(RecipientId);
        context.CharacterFriendships.Add(friendship);
        await context.SaveChangesAsync();

        var result = await new RemoveFriendCommandHandler(context, new TestCurrentUserService(RequesterId))
            .Handle(new RemoveFriendCommand(RecipientId), CancellationToken.None);
        var whisper = await new AuthorizeWhisperQueryHandler(context, new TestCurrentUserService(RequesterId))
            .Handle(new AuthorizeWhisperQuery(RecipientId), CancellationToken.None);

        Assert.Equal(FriendOperationStatusEnum.Applied, result.Status);
        Assert.False(whisper.IsAllowed);
        Assert.Equal(FriendOperationStatusEnum.WhisperNotAllowed, whisper.Status);
        Assert.Empty(context.CharacterFriendships);
    }

    [Fact]
    public async Task ConcurrentRemoval_ReturnsFriendshipNotFoundInsteadOfThrowing()
    {
        var databaseName = Guid.NewGuid().ToString();
        var winnerOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using (var seedContext = new ApplicationDbContext(winnerOptions))
        {
            seedContext.Characters.AddRange(
                CreateCharacter(RequesterId, "requester", "Requester"),
                CreateCharacter(RecipientId, "recipient", "Recipient"));
            var friendship = CharacterFriendship.Create(RequesterId, RecipientId);
            friendship.Accept(RecipientId);
            seedContext.CharacterFriendships.Add(friendship);

            await seedContext.SaveChangesAsync();
        }

        var racedOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(new RemoveFriendshipBeforeSaveInterceptor(winnerOptions))
            .Options;

        await using var racedContext = new ApplicationDbContext(racedOptions);

        var result = await new RemoveFriendCommandHandler(racedContext, new TestCurrentUserService(RequesterId))
            .Handle(new RemoveFriendCommand(RecipientId), CancellationToken.None);

        Assert.Equal(FriendOperationStatusEnum.FriendshipNotFound, result.Status);

        await using var verificationContext = new ApplicationDbContext(winnerOptions);
        Assert.Empty(verificationContext.CharacterFriendships);
    }

    [Fact]
    public async Task StaleDecline_CannotDeleteAnAcceptedFriendship()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            seedContext.Characters.AddRange(
                CreateCharacter(RequesterId, "requester", "Requester"),
                CreateCharacter(RecipientId, "recipient", "Recipient"));
            seedContext.CharacterFriendships.Add(CharacterFriendship.Create(RequesterId, RecipientId));

            await seedContext.SaveChangesAsync();
        }

        await using var acceptContext = new ApplicationDbContext(options);
        await using var declineContext = new ApplicationDbContext(options);
        var acceptedInvitation = await acceptContext.CharacterFriendships.SingleAsync();
        var declinedInvitation = await declineContext.CharacterFriendships.SingleAsync();

        acceptedInvitation.Accept(RecipientId);
        declineContext.CharacterFriendships.Remove(declinedInvitation);

        await acceptContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => declineContext.SaveChangesAsync());

        await using var verificationContext = new ApplicationDbContext(options);
        var friendship = await verificationContext.CharacterFriendships.SingleAsync();

        Assert.Equal(FriendshipStatusEnum.Accepted, friendship.Status);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Character CreateCharacter(int id, string userId, string name)
    {
        return new Character
        {
            Id = id,
            ApplicationUserId = userId,
            Name = name,
            Status = StatusEnum.Active
        };
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        private readonly int _characterId;

        public TestCurrentUserService(int characterId)
        {
            _characterId = characterId;
        }

        public LanguageEnum Language => LanguageEnum.en;

        public List<string>? Roles => [];

        public string GetId()
        {
            return $"user-{_characterId}";
        }

        public string GetAuthenticatedUserId()
        {
            return GetId();
        }

        public int? GetCharacterId()
        {
            return _characterId;
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

    private sealed class RemoveFriendshipBeforeSaveInterceptor : SaveChangesInterceptor
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private bool _removed;

        public RemoveFriendshipBeforeSaveInterceptor(DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (_removed)
            {
                return result;
            }

            _removed = true;

            await using var context = new ApplicationDbContext(_options);
            var friendship = await context.CharacterFriendships.SingleAsync(cancellationToken);

            context.CharacterFriendships.Remove(friendship);
            await context.SaveChangesAsync(cancellationToken);

            return result;
        }
    }
}
