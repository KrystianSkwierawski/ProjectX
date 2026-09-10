using FluentValidation;

namespace ProjectX.Application.Friends.Commands.SendFriendInvitation;

public class SendFriendInvitationCommandValidator : AbstractValidator<SendFriendInvitationCommand>
{
    public SendFriendInvitationCommandValidator()
    {
        RuleFor(x => x.CharacterName).NotEmpty().MaximumLength(100);
    }
}
