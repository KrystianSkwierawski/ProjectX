using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Shared.Enums;

namespace Assets.Scripts.Areas.Character.Models
{
    public class CharacterSettingsDto
    {
        public int CharacterId { get; set; }
        public LanguageEnum Language { get; set; }
        public InventoryItemEnum[] ActionBars { get; set; }
    }
}
