using FluentValidation;

namespace ProjectX.Application.Hideouts;

public class AccessHideoutCommandValidator : AbstractValidator<AccessHideoutCommand>
{
    public AccessHideoutCommandValidator()
    {
        RuleFor(x => x.Building).IsInEnum();
        RuleFor(x => x.Operation).IsInEnum();
        RuleFor(x => x.Item).IsInEnum();
        RuleFor(x => x.HarvestExperience).InclusiveBetween(0, 1000000);
        RuleFor(x => x.Count).InclusiveBetween(0, 1024);
        RuleFor(x => x.Revision).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RespawnInterval).InclusiveBetween(0.1, 31536000);
    }
}
