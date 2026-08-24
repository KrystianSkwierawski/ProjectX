using MediatR;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.GameSessions.Commands.RedeemGameSessionTicket;

public record RedeemGameSessionTicketCommand : IRequest<RedeemGameSessionTicketDto>
{
    public Guid GameSessionId { get; set; }

    public required string Ticket { get; set; }

    public override string ToString()
    {
        return $"{nameof(RedeemGameSessionTicketCommand)} {{ GameSessionId = {GameSessionId} }}";
    }
}

public class RedeemGameSessionTicketCommandHandler : IRequestHandler<RedeemGameSessionTicketCommand, RedeemGameSessionTicketDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IGameSessionService _gameSessionService;

    public RedeemGameSessionTicketCommandHandler(ICurrentUserService currentUserService, IGameSessionService gameSessionService)
    {
        _currentUserService = currentUserService;
        _gameSessionService = gameSessionService;
    }

    public Task<RedeemGameSessionTicketDto> Handle(RedeemGameSessionTicketCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var redeemed = _gameSessionService.Redeem(_currentUserService.GetAuthenticatedUserId(), request.GameSessionId, request.Ticket);

        return Task.FromResult(new RedeemGameSessionTicketDto
        {
            UserId = redeemed.UserId,
            CharacterId = redeemed.CharacterId,
            PlayerSessionId = redeemed.PlayerSessionId
        });
    }
}
