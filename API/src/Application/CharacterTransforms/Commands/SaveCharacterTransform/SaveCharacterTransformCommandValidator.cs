using FluentValidation;

namespace ProjectX.Application.CharacterTransforms.Commands.SaveCharacterTransform;

public sealed class SaveCharacterTransformCommandValidator : AbstractValidator<SaveCharacterTransformCommand>
{
    public SaveCharacterTransformCommandValidator()
    {
        // Temporary dungeon instances are not resumable. The server saves their world return point.
        RuleFor(x => x.SceneName).Equal("EnvironmentScene");

        RuleFor(x => x.PositionX).Must(float.IsFinite);
        RuleFor(x => x.PositionY).Must(float.IsFinite);
        RuleFor(x => x.PositionZ).Must(float.IsFinite);
        RuleFor(x => x.RotationY).Must(float.IsFinite);
    }
}
