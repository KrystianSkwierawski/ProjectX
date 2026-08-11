using System.Security.Cryptography;
using System.Text;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.GameSessions.Models;

namespace ProjectX.Infrastructure.GameSessions;

public sealed class InMemoryGameSessionService : IGameSessionService
{
    private const int SecretSizeBytes = 32;

    private readonly Dictionary<Guid, SessionState> _sessions = [];
    private readonly Dictionary<string, TicketState> _tickets = [];
    private readonly Dictionary<string, PlayerSessionState> _playerSessions = [];
    private readonly object _sync = new();
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _ticketLifetime;
    private readonly TimeSpan _serverLeaseLifetime;
    private readonly bool _allowDirectTransport;

    public InMemoryGameSessionService(TimeProvider timeProvider, TimeSpan ticketLifetime, TimeSpan serverLeaseLifetime, bool allowDirectTransport)
    {
        if (ticketLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ticketLifetime));
        }

        if (serverLeaseLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(serverLeaseLifetime));
        }

        _timeProvider = timeProvider;
        _ticketLifetime = ticketLifetime;
        _serverLeaseLifetime = serverLeaseLifetime;
        _allowDirectTransport = allowDirectTransport;
    }

    public RegisteredGameSession Register(string serverUserId, bool usesRelay, string? relayJoinCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUserId);

        if (usesRelay)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relayJoinCode);
        }
        else if (!_allowDirectTransport)
        {
            throw new ForbiddenAccessException();
        }

        lock (_sync)
        {
            var now = GetUtcNow();

            RemoveExpiredState(now);
            RemoveServerSessions(serverUserId);

            var session = new SessionState(Guid.NewGuid(), serverUserId, usesRelay, usesRelay ? relayJoinCode : null, now, now.Add(_serverLeaseLifetime));

            _sessions.Add(session.GameSessionId, session);

            return new RegisteredGameSession(session.GameSessionId, session.ExpiresAtUtc);
        }
    }

    public RegisteredGameSession Heartbeat(string serverUserId, Guid gameSessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUserId);

        if (gameSessionId == Guid.Empty)
        {
            throw new ArgumentException("The game-session identifier is required.", nameof(gameSessionId));
        }

        lock (_sync)
        {
            var now = GetUtcNow();

            RemoveExpiredState(now);

            if (!_sessions.TryGetValue(gameSessionId, out var session) || !string.Equals(session.ServerUserId, serverUserId, StringComparison.Ordinal))
            {
                throw new InvalidGameSessionCredentialException();
            }

            session = session with { ExpiresAtUtc = now.Add(_serverLeaseLifetime) };
            _sessions[gameSessionId] = session;

            return new RegisteredGameSession(session.GameSessionId, session.ExpiresAtUtc);
        }
    }

    public GameConnectionTicket CreateTicket(string clientUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientUserId);

        lock (_sync)
        {
            var now = GetUtcNow();

            RemoveExpiredState(now);

            var session = _sessions.Values
                .OrderByDescending(candidate => candidate.RegisteredAtUtc)
                .FirstOrDefault()
                ?? throw new NotFoundException("active game session");

            var existingTickets = _tickets.Where(pair => pair.Value.GameSessionId == session.GameSessionId && string.Equals(pair.Value.ClientUserId, clientUserId, StringComparison.Ordinal)).ToArray();

            foreach (var existingTicket in existingTickets)
            {
                _tickets.Remove(existingTicket.Key);
            }

            var ticket = CreateUniqueSecret(_tickets);
            var expiresAtUtc = now.Add(_ticketLifetime);
            _tickets.Add(Hash(ticket), new TicketState(session.GameSessionId, clientUserId, expiresAtUtc));

            return new GameConnectionTicket(session.GameSessionId, session.UsesRelay, session.RelayJoinCode, ticket, expiresAtUtc);
        }
    }

    public RedeemedGameSessionTicket Redeem(string serverUserId, Guid gameSessionId, string ticket)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ticket);

        lock (_sync)
        {
            var ticketHash = Hash(ticket);
            var now = GetUtcNow();

            RemoveExpiredState(now);

            if (!_tickets.TryGetValue(ticketHash, out var ticketState)
                || ticketState.GameSessionId != gameSessionId
                || !_sessions.TryGetValue(gameSessionId, out var session)
                || !string.Equals(session.ServerUserId, serverUserId, StringComparison.Ordinal))
            {
                throw new InvalidGameSessionCredentialException();
            }

            // Removing inside the same lock makes redemption single-use even for concurrent requests.
            _tickets.Remove(ticketHash);

            var playerSessionId = CreateUniqueSecret(_playerSessions);
            _playerSessions.Add(Hash(playerSessionId), new PlayerSessionState(gameSessionId, serverUserId, ticketState.ClientUserId));

            return new RedeemedGameSessionTicket(ticketState.ClientUserId, playerSessionId);
        }
    }

    public bool TryResolvePlayer(string serverUserId, string playerSessionId, out string userId)
    {
        userId = string.Empty;

        if (string.IsNullOrWhiteSpace(serverUserId) || string.IsNullOrWhiteSpace(playerSessionId))
        {
            return false;
        }

        lock (_sync)
        {
            RemoveExpiredState(GetUtcNow());

            if (!_playerSessions.TryGetValue(Hash(playerSessionId), out var playerSession)
                || !string.Equals(playerSession.ServerUserId, serverUserId, StringComparison.Ordinal)
                || !_sessions.ContainsKey(playerSession.GameSessionId))
            {
                return false;
            }

            userId = playerSession.ClientUserId;
            return true;
        }
    }

    public void RevokePlayer(string serverUserId, string playerSessionId)
    {
        if (string.IsNullOrWhiteSpace(serverUserId) || string.IsNullOrWhiteSpace(playerSessionId))
        {
            return;
        }

        lock (_sync)
        {
            RemoveExpiredState(GetUtcNow());

            var hash = Hash(playerSessionId);

            if (_playerSessions.TryGetValue(hash, out var playerSession) && string.Equals(playerSession.ServerUserId, serverUserId, StringComparison.Ordinal))
            {
                _playerSessions.Remove(hash);
            }
        }
    }

    private void RemoveServerSessions(string serverUserId)
    {
        var sessionIds = _sessions.Values
            .Where(session => string.Equals(session.ServerUserId, serverUserId, StringComparison.Ordinal))
            .Select(session => session.GameSessionId)
            .ToHashSet();

        RemoveSessions(sessionIds);
    }

    private void RemoveExpiredState(DateTimeOffset now)
    {
        var expiredSessionIds = _sessions.Values
            .Where(session => session.ExpiresAtUtc <= now)
            .Select(session => session.GameSessionId)
            .ToHashSet();

        RemoveSessions(expiredSessionIds);

        foreach (var ticket in _tickets.Where(pair => pair.Value.ExpiresAtUtc <= now).ToArray())
        {
            _tickets.Remove(ticket.Key);
        }
    }

    private void RemoveSessions(IReadOnlySet<Guid> sessionIds)
    {
        foreach (var sessionId in sessionIds)
        {
            _sessions.Remove(sessionId);
        }

        foreach (var ticket in _tickets.Where(pair => sessionIds.Contains(pair.Value.GameSessionId)).ToArray())
        {
            _tickets.Remove(ticket.Key);
        }

        foreach (var player in _playerSessions.Where(pair => sessionIds.Contains(pair.Value.GameSessionId)).ToArray())
        {
            _playerSessions.Remove(player.Key);
        }
    }

    private DateTimeOffset GetUtcNow()
    {
        return _timeProvider.GetUtcNow().ToUniversalTime();
    }

    private static string CreateUniqueSecret<TValue>(IReadOnlyDictionary<string, TValue> values)
    {
        while (true)
        {
            var secret = ToBase64Url(RandomNumberGenerator.GetBytes(SecretSizeBytes));

            if (!values.ContainsKey(Hash(secret)))
            {
                return secret;
            }
        }
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string ToBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed record SessionState(Guid GameSessionId, string ServerUserId, bool UsesRelay, string? RelayJoinCode, DateTimeOffset RegisteredAtUtc, DateTimeOffset ExpiresAtUtc);

    private sealed record TicketState(Guid GameSessionId, string ClientUserId, DateTimeOffset ExpiresAtUtc);

    private sealed record PlayerSessionState(Guid GameSessionId, string ServerUserId, string ClientUserId);
}
