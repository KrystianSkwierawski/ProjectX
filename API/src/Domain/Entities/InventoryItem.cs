using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class InventoryItem
{
    public InventoryItemEnum Id { get; set; }

    public string Name { get; set; }

    public byte MaxCount { get; set; }

    public DateTime ModDate { get; set; }
}
