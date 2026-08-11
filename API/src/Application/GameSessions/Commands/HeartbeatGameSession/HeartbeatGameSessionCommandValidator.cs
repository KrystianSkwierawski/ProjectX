using FluentValidation;

namespace ProjectX.Application.GameSessions.Commands.HeartbeatGameSession;

public class HeartbeatGameSessionCommandValidator : AbstractValidator<HeartbeatGameSessionCommand>
{
    public HeartbeatGameSessionCommandValidator()
    {
        RuleFor(command => command.GameSessionId).NotEmpty();
    }
}
