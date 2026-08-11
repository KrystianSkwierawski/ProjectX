namespace ProjectX.Application.GameSessions.Models;

public record RegisteredGameSession(Guid GameSessionId, DateTimeOffset ExpiresAtUtc);
