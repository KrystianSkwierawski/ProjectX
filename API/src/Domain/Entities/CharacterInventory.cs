using ProjectX.Domain.Common;

namespace ProjectX.Domain.Entities;

public class CharacterInventory : BaseAuditableEntity
{
    public int Id { get; set; }

    public required string Inventory { get; set; }

    public short Count { get; set; }

    public virtual Character Character { get; set; }
}
