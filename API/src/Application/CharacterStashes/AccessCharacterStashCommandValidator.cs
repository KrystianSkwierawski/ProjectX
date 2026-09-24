using FluentValidation;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterStashes;

public class AccessCharacterStashCommandValidator : AbstractValidator<AccessCharacterStashCommand>
{
    public AccessCharacterStashCommandValidator()
    {
        RuleFor(x => x.Operation).IsInEnum();
        RuleFor(x => x.Revision).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SourceIndex).InclusiveBetween(0, short.MaxValue - 1).When(x => x.Operation != StashOperationEnum.Read);
        RuleFor(x => x.ExpectedType).IsInEnum();
        RuleFor(x => x.ExpectedCount).InclusiveBetween(1, 1024).When(x => x.Operation != StashOperationEnum.Read);
        RuleFor(x => x.TargetIndex).InclusiveBetween(0, short.MaxValue - 1).When(x => x.TargetIndex.HasValue);
    }
}
