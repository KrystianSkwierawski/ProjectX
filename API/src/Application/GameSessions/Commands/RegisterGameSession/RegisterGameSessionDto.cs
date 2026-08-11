namespace ProjectX.Application.GameSessions.Commands.RegisterGameSession;

public class RegisterGameSessionDto
{
    public Guid GameSessionId { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
