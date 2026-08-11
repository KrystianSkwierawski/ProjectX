namespace ProjectX.Application.GameSessions.Models;

public record GameConnectionTicket(Guid GameSessionId, bool UsesRelay, string? RelayJoinCode, string Ticket, DateTimeOffset ExpiresAtUtc);
