using Assets.Scripts.Areas.Inventory.Enums;

namespace Assets.Scripts.Areas.Character.Models
{
    public class UpdateCharacterCommand
    {
        public int CharacterId { get; set; }

        public int? Health { get; set; }

        public short? Strength { get; set; }

        public short? Agility { get; set; }

        public short? Stamina { get; set; }

        public short? Intelligence { get; set; }

        public short? Spirit { get; set; }

        public short? Arrmor { get; set; }

        public InventoryItemEnum? Helmet { get; set; }

        public InventoryItemEnum? Chest { get; set; }

        public InventoryItemEnum? Boots { get; set; }

        public InventoryItemEnum? Weapon { get; set; }
    }
}
