using MediatR;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.GameSessions.Commands.RegisterGameSession;

public record RegisterGameSessionCommand : IRequest<RegisterGameSessionDto>
{
    public bool UsesRelay { get; set; }

    public string? RelayJoinCode { get; set; }

    public override string ToString()
    {
        return $"{nameof(RegisterGameSessionCommand)} {{ UsesRelay = {UsesRelay} }}";
    }
}

public class RegisterGameSessionCommandHandler : IRequestHandler<RegisterGameSessionCommand, RegisterGameSessionDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IGameSessionService _gameSessionService;

    public RegisterGameSessionCommandHandler(ICurrentUserService currentUserService, IGameSessionService gameSessionService)
    {
        _currentUserService = currentUserService;
        _gameSessionService = gameSessionService;
    }

    public Task<RegisterGameSessionDto> Handle(RegisterGameSessionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var session = _gameSessionService.Register(_currentUserService.GetAuthenticatedUserId(), request.UsesRelay, request.RelayJoinCode);

        return Task.FromResult(new RegisterGameSessionDto
        {
            GameSessionId = session.GameSessionId,
            ExpiresAtUtc = session.ExpiresAtUtc
        });
    }
}
