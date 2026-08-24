using ProjectX.Domain.Common;
using ProjectX.Domain.Inventory;

namespace ProjectX.Domain.Entities;

public class CharacterInventory : BaseAuditableEntity
{
    public int Id { get; set; }

    public InventoryState Inventory { get; set; } = new([]);

    public short Count { get; set; }

    public virtual Character Character { get; set; } = null!;
}
