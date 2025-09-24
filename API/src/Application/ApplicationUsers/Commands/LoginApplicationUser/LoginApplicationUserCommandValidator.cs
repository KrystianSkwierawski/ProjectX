using FluentValidation;

namespace ProjectX.Application.ApplicationUsers.Commands.LoginApplicationUser;
public class LoginApplicationUserCommandValidator : AbstractValidator<LoginApplicationUserCommand>
{
    public LoginApplicationUserCommandValidator()
    {
        RuleFor(x => x.UserName)
            .MaximumLength(256)
            .NotEmpty();

        RuleFor(x => x.Password)
            .MinimumLength(6)
           .MaximumLength(256)
           .NotEmpty();
    }
}
