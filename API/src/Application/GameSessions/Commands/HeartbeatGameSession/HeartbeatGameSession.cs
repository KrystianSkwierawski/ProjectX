using MediatR;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.GameSessions.Commands.HeartbeatGameSession;

public record HeartbeatGameSessionCommand : IRequest<HeartbeatGameSessionDto>
{
    public Guid GameSessionId { get; set; }

    public override string ToString()
    {
        return $"{nameof(HeartbeatGameSessionCommand)} {{ GameSessionId = {GameSessionId} }}";
    }
}

public class HeartbeatGameSessionCommandHandler : IRequestHandler<HeartbeatGameSessionCommand, HeartbeatGameSessionDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IGameSessionService _gameSessionService;

    public HeartbeatGameSessionCommandHandler(ICurrentUserService currentUserService, IGameSessionService gameSessionService)
    {
        _currentUserService = currentUserService;
        _gameSessionService = gameSessionService;
    }

    public Task<HeartbeatGameSessionDto> Handle(HeartbeatGameSessionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var session = _gameSessionService.Heartbeat(_currentUserService.GetAuthenticatedUserId(), request.GameSessionId);

        return Task.FromResult(new HeartbeatGameSessionDto
        {
            GameSessionId = session.GameSessionId,
            ExpiresAtUtc = session.ExpiresAtUtc
        });
    }
}
