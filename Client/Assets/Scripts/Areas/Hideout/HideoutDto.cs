using System;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Enums;

namespace Assets.Scripts.Areas.Hideout
{
    public enum HideoutBuildingEnum
    {
        None = 0,
        Farm = 1
    }

    public enum HideoutOperationEnum
    {
        Build = 0,
        Read = 1,
        Upgrade = 2,
        Deposit = 3,
        Withdraw = 4,
        Harvest = 5
    }

    public sealed class FarmStateDto
    {
        public int Level { get; set; } = 1;

        public DateTimeOffset? UpgradeEndsAt { get; set; }

        public long Revision { get; set; }

        public FarmSlotDto[] Slots { get; set; } = Array.Empty<FarmSlotDto>();

        public int Capacity => Level * 2;
    }

    public sealed class FarmSlotDto
    {
        public InventoryItemDto Seeds { get; set; }

        public InventoryItemEnum Ready { get; set; }

        public DateTimeOffset? ReadyAt { get; set; }
    }

    public class HideoutDto
    {
        public string Outcome { get; set; }

        public DateTimeOffset CurrentTime { get; set; }

        public byte HerbalismLevel { get; set; }

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

        public FarmStateDto Farm { get; set; }

        public HideoutRequirementDto UpgradeRequirement { get; set; }

        public int UpgradeTime { get; set; }
    }

    public class HideoutRequirementDto
    {
        public InventoryItemDto[] Items { get; set; }

        public int Level { get; set; }
    }
}
