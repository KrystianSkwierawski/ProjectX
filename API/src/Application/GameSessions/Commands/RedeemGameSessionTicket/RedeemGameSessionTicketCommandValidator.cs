using FluentValidation;

namespace ProjectX.Application.GameSessions.Commands.RedeemGameSessionTicket;

public class RedeemGameSessionTicketCommandValidator : AbstractValidator<RedeemGameSessionTicketCommand>
{
    public RedeemGameSessionTicketCommandValidator()
    {
        RuleFor(command => command.GameSessionId).NotEmpty();
        RuleFor(command => command.Ticket).NotEmpty().MaximumLength(256);
    }
}
