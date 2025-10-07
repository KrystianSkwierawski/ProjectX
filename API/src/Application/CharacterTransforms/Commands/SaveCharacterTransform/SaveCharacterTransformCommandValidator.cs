using FluentValidation;

namespace ProjectX.Application.CharacterTransforms.Commands.SaveCharacterTransform;
public class SaveCharacterTransformCommandValidator : AbstractValidator<SaveTransformTransformCommand>
{
    public SaveCharacterTransformCommandValidator()
    {
        RuleFor(x => x.ClientToken)
            .NotNull()
            .NotEmpty();
    }
}
