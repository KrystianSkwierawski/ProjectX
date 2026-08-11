using FluentValidation;

namespace ProjectX.Application.GameSessions.Commands.RegisterGameSession;

public class RegisterGameSessionCommandValidator : AbstractValidator<RegisterGameSessionCommand>
{
    public RegisterGameSessionCommandValidator()
    {
        RuleFor(command => command.RelayJoinCode)
            .NotEmpty()
            .MaximumLength(128)
            .When(command => command.UsesRelay);

        RuleFor(command => command.RelayJoinCode)
            .Empty()
            .When(command => !command.UsesRelay);
    }
}
