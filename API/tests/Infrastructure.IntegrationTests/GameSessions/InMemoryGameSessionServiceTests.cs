using ProjectX.Application.Common.Exceptions;
using ProjectX.Infrastructure.GameSessions;

namespace ProjectX.Infrastructure.IntegrationTests.GameSessions;

public sealed class InMemoryGameSessionServiceTests
{
    private const string ServerUserId = "server-user";
    private const string ClientUserId = "client-user";
    private const int CharacterId = 42;

    [Fact]
    public void Register_ReturnsUtcLeaseExpiry()
    {
        var service = CreateService(out var timeProvider);

        var session = service.Register(ServerUserId, false, null);

        Assert.Equal(timeProvider.GetUtcNow().AddSeconds(90), session.ExpiresAtUtc);
        Assert.Equal(TimeSpan.Zero, session.ExpiresAtUtc.Offset);
    }

    [Fact]
    public void Heartbeat_ExtendsLeaseFromCurrentUtcTime()
    {
        var service = CreateService(out var timeProvider);
        var session = service.Register(ServerUserId, false, null);
        timeProvider.Advance(TimeSpan.FromSeconds(30));

        var renewed = service.Heartbeat(ServerUserId, session.GameSessionId);

        Assert.Equal(session.GameSessionId, renewed.GameSessionId);
        Assert.Equal(timeProvider.GetUtcNow().AddSeconds(90), renewed.ExpiresAtUtc);
        Assert.True(renewed.ExpiresAtUtc > session.ExpiresAtUtc);
        Assert.Equal(TimeSpan.Zero, renewed.ExpiresAtUtc.Offset);
    }

    [Fact]
    public void Heartbeat_WrongServerCannotRenewLease()
    {
        var service = CreateService(out var timeProvider);
        var session = service.Register(ServerUserId, false, null);
        timeProvider.Advance(TimeSpan.FromSeconds(60));

        Assert.Throws<InvalidGameSessionCredentialException>(() => service.Heartbeat("another-server", session.GameSessionId));

        timeProvider.Advance(TimeSpan.FromSeconds(30));
        Assert.Throws<InvalidGameSessionCredentialException>(() => service.Heartbeat(ServerUserId, session.GameSessionId));
    }

    [Fact]
    public void ExpiredSession_RemovesTicketsAndPlayerSessions()
    {
        var service = CreateService(out var timeProvider);
        var session = service.Register(ServerUserId, false, null);
        var redeemedTicket = service.CreateTicket(ClientUserId, CharacterId);
        var player = service.Redeem(ServerUserId, session.GameSessionId, redeemedTicket.Ticket);
        var outstandingTicket = service.CreateTicket("another-client", 43);

        timeProvider.Advance(TimeSpan.FromSeconds(90));

        Assert.False(service.TryResolvePlayer(ServerUserId, player.PlayerSessionId, out _));
        Assert.Throws<InvalidGameSessionCredentialException>(() => service.Redeem(ServerUserId, session.GameSessionId, outstandingTicket.Ticket));
        Assert.Throws<NotFoundException>(() => service.CreateTicket(ClientUserId, CharacterId));
    }

    [Fact]
    public void Register_RejectsDirectTransportWhenItIsDisabled()
    {
        var service = CreateService(out _, allowDirectTransport: false);

        Assert.Throws<ForbiddenAccessException>(() => service.Register(ServerUserId, false, null));

        var relaySession = service.Register(ServerUserId, true, "AB12CD");
        Assert.NotEqual(Guid.Empty, relaySession.GameSessionId);
    }

    [Fact]
    public void CreateAndRedeemTicket_IssuesServerScopedPlayerSession()
    {
        var service = CreateService(out _);
        var session = service.Register(ServerUserId, true, "AB12CD");
        var ticket = service.CreateTicket(ClientUserId, CharacterId);

        var redeemed = service.Redeem(ServerUserId, session.GameSessionId, ticket.Ticket);

        Assert.Equal(session.GameSessionId, ticket.GameSessionId);
        Assert.Equal(CharacterId, ticket.CharacterId);
        Assert.True(ticket.UsesRelay);
        Assert.Equal("AB12CD", ticket.RelayJoinCode);
        Assert.Equal(ClientUserId, redeemed.UserId);
        Assert.Equal(CharacterId, redeemed.CharacterId);
        Assert.True(service.TryResolvePlayer(ServerUserId, redeemed.PlayerSessionId, out var playerSession));
        Assert.Equal(ClientUserId, playerSession.UserId);
        Assert.Equal(CharacterId, playerSession.CharacterId);
        Assert.False(service.TryResolvePlayer("another-server", redeemed.PlayerSessionId, out _));
    }

    [Fact]
    public void Redeem_ConsumesTicketExactlyOnce()
    {
        var service = CreateService(out _);
        var session = service.Register(ServerUserId, false, null);
        var ticket = service.CreateTicket(ClientUserId, CharacterId);

        service.Redeem(ServerUserId, session.GameSessionId, ticket.Ticket);

        Assert.Throws<InvalidGameSessionCredentialException>(() => service.Redeem(ServerUserId, session.GameSessionId, ticket.Ticket));
    }

    [Fact]
    public void Redeem_AllowsOnlyOneConcurrentConsumer()
    {
        var service = CreateService(out _);
        var session = service.Register(ServerUserId, false, null);
        var ticket = service.CreateTicket(ClientUserId, CharacterId);

        var successfulRedemptions = Enumerable.Range(0, 16)
            .AsParallel()
            .Count(_ =>
            {
                try
                {
                    service.Redeem(ServerUserId, session.GameSessionId, ticket.Ticket);
                    return true;
                }
                catch (InvalidGameSessionCredentialException)
                {
                    return false;
                }
            });

        Assert.Equal(1, successfulRedemptions);
    }

    [Fact]
    public void Redeem_RejectsTicketAtExpiryBoundary()
    {
        var service = CreateService(out var timeProvider);
        var session = service.Register(ServerUserId, false, null);
        var ticket = service.CreateTicket(ClientUserId, CharacterId);

        timeProvider.Advance(TimeSpan.FromSeconds(60));

        Assert.Equal(TimeSpan.Zero, ticket.ExpiresAtUtc.Offset);
        Assert.Throws<InvalidGameSessionCredentialException>(() => service.Redeem(ServerUserId, session.GameSessionId, ticket.Ticket));
    }

    [Fact]
    public void Redeem_WrongServerDoesNotConsumeTicket()
    {
        var service = CreateService(out _);
        var session = service.Register(ServerUserId, false, null);
        var ticket = service.CreateTicket(ClientUserId, CharacterId);

        Assert.Throws<InvalidGameSessionCredentialException>(() => service.Redeem("another-server", session.GameSessionId, ticket.Ticket));

        var redeemed = service.Redeem(ServerUserId, session.GameSessionId, ticket.Ticket);
        Assert.Equal(ClientUserId, redeemed.UserId);
    }

    [Fact]
    public void CreateTicket_ReplacesOnlyPreviousTicketForSameClientAndSession()
    {
        var service = CreateService(out _);
        var session = service.Register(ServerUserId, false, null);
        var replacedTicket = service.CreateTicket(ClientUserId, CharacterId);
        var otherClientTicket = service.CreateTicket("another-client", 43);

        var currentTicket = service.CreateTicket(ClientUserId, CharacterId);

        Assert.Throws<InvalidGameSessionCredentialException>(() => service.Redeem(ServerUserId, session.GameSessionId, replacedTicket.Ticket));
        Assert.Equal("another-client", service.Redeem(ServerUserId, session.GameSessionId, otherClientTicket.Ticket).UserId);
        Assert.Equal(ClientUserId, service.Redeem(ServerUserId, session.GameSessionId, currentTicket.Ticket).UserId);
    }

    [Fact]
    public void Register_ReplacesPreviousSessionAndRevokesItsPlayerSessions()
    {
        var service = CreateService(out _);
        var oldSession = service.Register(ServerUserId, false, null);
        var ticket = service.CreateTicket(ClientUserId, CharacterId);
        var player = service.Redeem(ServerUserId, oldSession.GameSessionId, ticket.Ticket);

        var newSession = service.Register(ServerUserId, true, "NEW123");

        Assert.NotEqual(oldSession.GameSessionId, newSession.GameSessionId);
        Assert.False(service.TryResolvePlayer(ServerUserId, player.PlayerSessionId, out _));
    }

    [Fact]
    public void RevokePlayer_InvalidatesPlayerSession()
    {
        var service = CreateService(out _);
        var session = service.Register(ServerUserId, false, null);
        var ticket = service.CreateTicket(ClientUserId, CharacterId);
        var player = service.Redeem(ServerUserId, session.GameSessionId, ticket.Ticket);

        service.RevokePlayer(ServerUserId, player.PlayerSessionId);

        Assert.False(service.TryResolvePlayer(ServerUserId, player.PlayerSessionId, out _));
    }

    [Fact]
    public void IsCharacterOnline_TracksRedeemedPlayerSessionLifetime()
    {
        var service = CreateService(out var timeProvider);
        var session = service.Register(ServerUserId, false, null);
        var ticket = service.CreateTicket(ClientUserId, CharacterId);

        var player = service.Redeem(ServerUserId, session.GameSessionId, ticket.Ticket);

        Assert.True(service.IsCharacterOnline(ServerUserId, CharacterId));
        Assert.False(service.IsCharacterOnline("another-server", CharacterId));

        service.RevokePlayer(ServerUserId, player.PlayerSessionId);

        Assert.False(service.IsCharacterOnline(ServerUserId, CharacterId));

        var secondTicket = service.CreateTicket(ClientUserId, CharacterId);
        service.Redeem(ServerUserId, session.GameSessionId, secondTicket.Ticket);
        timeProvider.Advance(TimeSpan.FromSeconds(90));

        Assert.False(service.IsCharacterOnline(ServerUserId, CharacterId));
    }

    [Fact]
    public void IsCharacterOnline_DoesNotExposePlayersFromAnotherServerSession()
    {
        var service = CreateService(out var timeProvider);
        var firstSession = service.Register(ServerUserId, false, null);
        var firstTicket = service.CreateTicket(ClientUserId, CharacterId);

        service.Redeem(ServerUserId, firstSession.GameSessionId, firstTicket.Ticket);

        timeProvider.Advance(TimeSpan.FromSeconds(1));

        const string secondServerUserId = "another-server";
        const int secondCharacterId = 43;
        var secondSession = service.Register(secondServerUserId, false, null);
        var secondTicket = service.CreateTicket("another-client", secondCharacterId);

        service.Redeem(secondServerUserId, secondSession.GameSessionId, secondTicket.Ticket);

        Assert.True(service.IsCharacterOnline(ServerUserId, CharacterId));
        Assert.False(service.IsCharacterOnline(secondServerUserId, CharacterId));
        Assert.True(service.IsCharacterOnline(secondServerUserId, secondCharacterId));
        Assert.False(service.IsCharacterOnline(ServerUserId, secondCharacterId));
    }

    private static InMemoryGameSessionService CreateService(out ManualTimeProvider timeProvider, bool allowDirectTransport = true)
    {
        timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));

        return new InMemoryGameSessionService(timeProvider, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(90), allowDirectTransport);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
