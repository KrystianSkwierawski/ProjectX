using FluentValidation;

namespace ProjectX.Application.CharacterInventories.Commands.ResolveCharacterInventoryTrade;

public class ResolveCharacterInventoryTradeCommandValidator : AbstractValidator<ResolveCharacterInventoryTradeCommand>
{
    public ResolveCharacterInventoryTradeCommandValidator()
    {
        RuleFor(x => x.TradeId).NotEmpty();
    }
}
