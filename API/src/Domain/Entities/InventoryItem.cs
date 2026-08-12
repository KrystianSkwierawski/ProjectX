using ProjectX.Domain.Common;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class InventoryItem : BaseAuditableEntity
{
    public InventoryItemEnum Id { get; set; }

    public required string Name { get; set; }

    public byte MaxCount { get; set; }

}
