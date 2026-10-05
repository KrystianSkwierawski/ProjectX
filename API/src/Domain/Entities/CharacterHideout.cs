using ProjectX.Domain.Common;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class CharacterHideout : BaseAuditableEntity
{
    public int Id { get; set; }

    public int CharacterId { get; set; }

    public Character Character { get; set; } = null!;

    public HideoutBuildingEnum HideoutBuildingTypeId { get; set; }

    public HideoutBuildingType HideoutBuildingType { get; set; } = null!;

    public DateTimeOffset BuildStartedAt { get; set; }

    public DateTimeOffset BuildEndsAt { get; set; }

    public bool IsUnderConstruction(DateTimeOffset now) => now < BuildEndsAt;
}
