namespace ProjectX.Application.GameSessions.Commands.RedeemGameSessionTicket;

public class RedeemGameSessionTicketDto
{
    public required string UserId { get; set; }

    public required string PlayerSessionId { get; set; }

    public override string ToString()
    {
        return $"{nameof(RedeemGameSessionTicketDto)} {{ UserId = {UserId} }}";
    }
}
