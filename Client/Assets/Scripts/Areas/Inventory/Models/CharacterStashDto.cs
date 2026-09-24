using Assets.Scripts.Areas.Inventory.Enums;

namespace Assets.Scripts.Areas.Inventory.Models
{
    public class AccessCharacterStashCommand
    {
        public StashOperationEnum Operation { get; set; }

        public long Revision { get; set; }

        public int SourceIndex { get; set; } = -1;

        public int? TargetIndex { get; set; }

        public InventoryItemEnum ExpectedType { get; set; }

        public int ExpectedCount { get; set; }
    }

    public class CharacterStashDto
    {
        public StashStatusEnum Status { get; set; }

        public long Revision { get; set; }

        public short Count { get; set; }

        public InventoryDto Inventory { get; set; }

        public CharacterInventoryDto CharacterInventory { get; set; }
    }
}
