using FluentValidation;

namespace ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
public class AddCharacterExperienceCommandValidator : AbstractValidator<AddCharacterExperienceCommand>
{
    public AddCharacterExperienceCommandValidator()
    {
        RuleFor(x => x.Amount)
            .LessThan(10000)
            .GreaterThan(0);

        RuleFor(x => x.ClientToken)
            .NotNull()
            .NotEmpty();
    }
}
