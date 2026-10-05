using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterStashes;

public class CharacterStashDto
{
    public StashStatusEnum Status { get; set; }

    public long Revision { get; set; }

    public short Count { get; set; } = 64;

    public InventoryDto? Inventory { get; set; }

    public CharacterInventoryDto? CharacterInventory { get; set; }
}
