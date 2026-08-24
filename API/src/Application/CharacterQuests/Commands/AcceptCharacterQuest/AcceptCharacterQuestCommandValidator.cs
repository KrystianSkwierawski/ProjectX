using FluentValidation;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;

public sealed class AcceptCharacterQuestCommandValidator : AbstractValidator<AcceptCharacterQuestCommand>
{
    public AcceptCharacterQuestCommandValidator()
    {
        RuleFor(x => x.QuestId)
            .Must(x => x != QuestEnum.None && Enum.IsDefined(x));
    }
}
