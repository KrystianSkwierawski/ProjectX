using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
public class CharacterInventoryDto
{
    public int CharacterId { get; set; }

    public required InventoryDto Inventory { get; set; }

    public short Count { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterInventoryDto)} {{ CharacterId = {CharacterId}, Count = {Count} }}";
    }
}

public class InventoryDto
{
    public IList<InventoryItem> Items { get; set; } = [];
}

public class InventoryItem
{
    public CharacterInventoryTypeEnum Type { get; set; }

    public int Count { get; set; }
}