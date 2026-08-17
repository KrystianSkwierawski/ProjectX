using Assets.Scripts.Areas.Inventory.Enums;

namespace Assets.Scripts.Areas.Character.Models
{
    public class UpdateCharacterCommand
    {
        public int? Health { get; set; }

        public int? MaxHealth { get; set; }

        public short? Strength { get; set; }

        public short? Dexterity { get; set; }

        public short? Speed { get; set; }

        public short? Intellect { get; set; }

        public short? Armor { get; set; }

        public InventoryItemEnum? HelmetType { get; set; }

        public InventoryItemEnum? ChestType { get; set; }

        public InventoryItemEnum? BootsType { get; set; }

        public InventoryItemEnum? WeaponType { get; set; }

        public InventoryItemEnum? AmmoType { get; set; }

        public int? AmmoCount { get; set; }
    }
}
