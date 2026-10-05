using System;
using Assets.Scripts.Areas.Inventory.Models;

namespace Assets.Scripts.Areas.Hideout
{
    public enum HideoutBuildingEnum
    {
        None = 0,
        ChamomileFarm = 1
    }

    public class HideoutDto
    {
        public string Outcome { get; set; }

        public DateTimeOffset CurrentTime { get; set; }

        public HideoutBuildingDto[] Buildings { get; set; }

        public CharacterInventoryDto CharacterInventory { get; set; }
    }

    public class HideoutBuildingDto
    {
        public HideoutBuildingEnum Id { get; set; }

        public string Name { get; set; }

        public HideoutRequirementDto Requirement { get; set; }

        public int BuildTime { get; set; }

        public DateTimeOffset? BuildStartedAt { get; set; }

        public DateTimeOffset? BuildEndsAt { get; set; }
    }

    public class HideoutRequirementDto
    {
        public InventoryItemDto[] Items { get; set; }

        public int Level { get; set; }
    }
}
