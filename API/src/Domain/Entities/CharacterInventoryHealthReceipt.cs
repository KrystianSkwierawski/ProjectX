using ProjectX.Domain.Common;

namespace ProjectX.Domain.Entities;

public sealed class CharacterInventoryHealthReceipt : BaseAuditableEntity
{
    public Guid Id { get; set; }

    public int CharacterId { get; set; }

    public string RequestFingerprint { get; set; } = string.Empty;

    public bool Applied { get; set; }
}
