using FluentValidation;

namespace ProjectX.Application.Friends.Commands.RespondFriendInvitation;

public class RespondFriendInvitationCommandValidator : AbstractValidator<RespondFriendInvitationCommand>
{
    public RespondFriendInvitationCommandValidator()
    {
        RuleFor(x => x.CharacterId).GreaterThan(0);
    }
}
