namespace ProjectX.Domain.Entities;
public class CharacterInventory
{
    public int Id { get; set; }

    public required string Inventory { get; set; }

    public DateTime ModDate { get; set; }

    public short Count { get; set; }

    public virtual Character Character { get; set; }
}
