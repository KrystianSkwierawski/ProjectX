using ProjectX.Domain.Common;
using ProjectX.Domain.Inventory;

namespace ProjectX.Domain.Entities;

public class CharacterStash : BaseAuditableEntity
{
    public string ApplicationUserId { get; set; } = null!;

    public InventoryState Inventory { get; set; } = new([]);

    public short Count { get; set; } = 64;

    public long Revision { get; set; }
}
