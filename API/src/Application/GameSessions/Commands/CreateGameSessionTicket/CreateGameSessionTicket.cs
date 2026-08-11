using MediatR;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.GameSessions.Commands.CreateGameSessionTicket;

public record CreateGameSessionTicketCommand : IRequest<CreateGameSessionTicketDto>
{
    public override string ToString()
    {
        return nameof(CreateGameSessionTicketCommand);
    }
}

public class CreateGameSessionTicketCommandHandler : IRequestHandler<CreateGameSessionTicketCommand, CreateGameSessionTicketDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IGameSessionService _gameSessionService;

    public CreateGameSessionTicketCommandHandler(ICurrentUserService currentUserService, IGameSessionService gameSessionService)
    {
        _currentUserService = currentUserService;
        _gameSessionService = gameSessionService;
    }

    public Task<CreateGameSessionTicketDto> Handle(CreateGameSessionTicketCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ticket = _gameSessionService.CreateTicket(_currentUserService.GetAuthenticatedUserId());

        return Task.FromResult(new CreateGameSessionTicketDto
        {
            GameSessionId = ticket.GameSessionId,
            UsesRelay = ticket.UsesRelay,
            RelayJoinCode = ticket.RelayJoinCode,
            Ticket = ticket.Ticket,
            ExpiresAtUtc = ticket.ExpiresAtUtc
        });
    }
}
