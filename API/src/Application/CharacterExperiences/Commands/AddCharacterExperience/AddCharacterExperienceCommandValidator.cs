using FluentValidation;

namespace ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
public class AddCharacterExperienceCommandValidator : AbstractValidator<AddCharacterExperienceCommand>
{
    public AddCharacterExperienceCommandValidator()
    {
        RuleFor(x => x.ClientToken)
            .NotNull()
            .NotEmpty();
    }
}
