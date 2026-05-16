using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Professions.Models
{
    public class CraftingRecipeRequirementDto
    {
        public InventoryItemDto[] Items { get; set; }

        public int Level { get; set; }
    }
}
