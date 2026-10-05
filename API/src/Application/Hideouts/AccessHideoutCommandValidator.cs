using FluentValidation;

namespace ProjectX.Application.Hideouts;

public class AccessHideoutCommandValidator : AbstractValidator<AccessHideoutCommand>
{
    public AccessHideoutCommandValidator()
    {
        RuleFor(x => x.Building).IsInEnum();
    }
}
