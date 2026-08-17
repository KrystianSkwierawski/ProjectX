namespace ProjectX.Application.GameSessions.Models;

public record GameConnectionTicket(
    Guid GameSessionId,
    int CharacterId,
    bool UsesRelay,
    string? RelayJoinCode,
    string Ticket,
    DateTimeOffset ExpiresAtUtc);
