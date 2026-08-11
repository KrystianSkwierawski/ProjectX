using FluentValidation;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;

public sealed class AcceptCharacterQuestCommandValidator : AbstractValidator<AcceptCharacterQuestCommand>
{
    public AcceptCharacterQuestCommandValidator()
    {
        RuleFor(command => command.QuestId)
            .Must(questId => questId != QuestEnum.None && Enum.IsDefined(questId));
    }
}
