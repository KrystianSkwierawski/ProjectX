namespace ProjectX.Application.GameSessions.Commands.CreateGameSessionTicket;

public class CreateGameSessionTicketDto
{
    public Guid GameSessionId { get; set; }

    public bool UsesRelay { get; set; }

    public string? RelayJoinCode { get; set; }

    public required string Ticket { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public override string ToString()
    {
        return $"{nameof(CreateGameSessionTicketDto)} {{ GameSessionId = {GameSessionId}, UsesRelay = {UsesRelay}, ExpiresAtUtc = {ExpiresAtUtc:O} }}";
    }
}
