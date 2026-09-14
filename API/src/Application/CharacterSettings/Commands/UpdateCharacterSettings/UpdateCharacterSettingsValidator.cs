using FluentValidation;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterSettings.Commands.UpdateCharacterSettings;

public class UpdateCharacterSettingsValidator : AbstractValidator<UpdateCharacterSettingsCommand>
{
    public UpdateCharacterSettingsValidator()
    {
        RuleFor(x => x.CharacterId).GreaterThan(0);

        RuleFor(x => x.Language).IsInEnum();

        RuleFor(x => x.ActionBars).NotNull()
            .Must(x => x is { Length: Domain.Entities.CharacterSettings.ActionBarSlotCount })
            .WithMessage("Exactly ten action bar slots are required.");

        RuleForEach(x => x.ActionBars)
            .Must(x => x == InventoryItemEnum.None || Domain.Entities.CharacterSettings.IsActionBarItem(x))
            .WithMessage("Only usable items can be assigned to action bars.");
    }
}
