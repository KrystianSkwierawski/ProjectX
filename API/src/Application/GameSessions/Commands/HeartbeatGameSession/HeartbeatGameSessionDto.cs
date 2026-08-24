namespace ProjectX.Application.GameSessions.Commands.HeartbeatGameSession;

public class HeartbeatGameSessionDto
{
    public Guid GameSessionId { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
