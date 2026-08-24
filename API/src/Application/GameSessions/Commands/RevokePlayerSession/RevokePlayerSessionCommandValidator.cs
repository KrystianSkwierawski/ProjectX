using FluentValidation;

namespace ProjectX.Application.GameSessions.Commands.RevokePlayerSession;

public class RevokePlayerSessionCommandValidator : AbstractValidator<RevokePlayerSessionCommand>
{
    public RevokePlayerSessionCommandValidator()
    {
        RuleFor(command => command.PlayerSessionId).NotEmpty().MaximumLength(256);
    }
}
