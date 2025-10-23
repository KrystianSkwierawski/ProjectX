using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Domain.Entities;
public class CharacterInventory
{
    [Key]
    [ForeignKey(nameof(Character))]
    public int CharacterId { get; set; }

    public required string Items { get; set; }

    public DateTime ModDate { get; set; }

    public virtual Character Character { get; set; }
}
