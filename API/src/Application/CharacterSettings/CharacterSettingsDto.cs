using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterSettings;

public class CharacterSettingsDto
{
    public int CharacterId { get; set; }

    public LanguageEnum Language { get; set; }

    public InventoryItemEnum[] ActionBars { get; set; } = [];
}
