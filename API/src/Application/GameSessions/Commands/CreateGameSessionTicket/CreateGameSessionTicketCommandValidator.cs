using FluentValidation;

namespace ProjectX.Application.GameSessions.Commands.CreateGameSessionTicket;

public sealed class CreateGameSessionTicketCommandValidator : AbstractValidator<CreateGameSessionTicketCommand>
{
    public CreateGameSessionTicketCommandValidator()
    {
        RuleFor(x => x.CharacterId)
            .GreaterThan(0);
    }
}
