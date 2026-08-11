using MediatR;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.GameSessions.Commands.RevokePlayerSession;

public record RevokePlayerSessionCommand : IRequest
{
    public required string PlayerSessionId { get; set; }

    public override string ToString()
    {
        return nameof(RevokePlayerSessionCommand);
    }
}

public class RevokePlayerSessionCommandHandler : IRequestHandler<RevokePlayerSessionCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IGameSessionService _gameSessionService;

    public RevokePlayerSessionCommandHandler(ICurrentUserService currentUserService, IGameSessionService gameSessionService)
    {
        _currentUserService = currentUserService;
        _gameSessionService = gameSessionService;
    }

    public Task Handle(RevokePlayerSessionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _gameSessionService.RevokePlayer(_currentUserService.GetAuthenticatedUserId(), request.PlayerSessionId);

        return Task.CompletedTask;
    }
}
