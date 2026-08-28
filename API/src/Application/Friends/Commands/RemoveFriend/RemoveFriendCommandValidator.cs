using FluentValidation;

namespace ProjectX.Application.Friends.Commands.RemoveFriend;

public class RemoveFriendCommandValidator : AbstractValidator<RemoveFriendCommand>
{
    public RemoveFriendCommandValidator()
    {
        RuleFor(x => x.CharacterId).GreaterThan(0);
    }
}
