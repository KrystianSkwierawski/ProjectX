namespace ProjectX.Application.CharacterInventories.Commands.UpdateCharacterInventory;

public class UpdateCharacterInventoryDto
{
    public UpdateCharacterInventoryStatusEnum Status { get; set; }
}

public enum UpdateCharacterInventoryStatusEnum
{
    Applied,
    InventoryFull
}
