using FluentValidation;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterInventories.Commands.TradeCharacterInventories;

public class TradeCharacterInventoriesCommandValidator : AbstractValidator<TradeCharacterInventoriesCommand>
{
    public TradeCharacterInventoriesCommandValidator()
    {
        RuleFor(x => x.TradeId).NotEmpty();
        RuleFor(x => x.OtherPlayerSessionId).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Give).NotNull();
        RuleFor(x => x.Receive).NotNull();
        RuleFor(x => x).Must(x => (x.Give?.Length ?? 0) > 0 || (x.Receive?.Length ?? 0) > 0);

        RuleForEach(x => x.Give).ChildRules(ValidateItem);
        RuleForEach(x => x.Receive).ChildRules(ValidateItem);
    }

    private static void ValidateItem(InlineValidator<InventoryItemDto> item)
    {
        item.RuleFor(x => x.Type).IsInEnum().NotEqual(InventoryItemEnum.None);
        item.RuleFor(x => x.Count).GreaterThan(0);
    }
}
