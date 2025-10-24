using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Domain.Entities;
public class CharacterInventory
{
    [Key]
    [ForeignKey(nameof(Character))]
    public int CharacterId { get; set; }

    public required string Inventory { get; set; }

    public DateTime ModDate { get; set; }

    public short Count { get; set; }

    public virtual Character Character { get; set; }
}
