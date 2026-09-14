using ProjectX.Domain.Common;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class CharacterSettings : BaseAuditableEntity
{
    public const int ActionBarSlotCount = 10;

    public int CharacterId { get; set; }

    public Character Character { get; set; } = null!;

    public LanguageEnum Language { get; private set; } = LanguageEnum.en;

    public InventoryItemEnum[] ActionBars { get; private set; } = new InventoryItemEnum[ActionBarSlotCount];

    public static bool IsActionBarItem(InventoryItemEnum type) =>
        type is InventoryItemEnum.Currency or InventoryItemEnum.HealthPotion or InventoryItemEnum.StrengthPotion or InventoryItemEnum.SpeedPotion
        || Enum.IsDefined(type) && type >= InventoryItemEnum.IronHelmet && type <= InventoryItemEnum.IronBow;

    public void Update(LanguageEnum language, IReadOnlyList<InventoryItemEnum> actionBars)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language));
        }

        if (actionBars.Count != ActionBarSlotCount
            || actionBars.Any(x => x != InventoryItemEnum.None && !IsActionBarItem(x)))
        {
            throw new ArgumentException("Action bars require exactly ten empty or usable item slots.", nameof(actionBars));
        }

        Language = language;
        ActionBars = actionBars.ToArray();
    }
}
