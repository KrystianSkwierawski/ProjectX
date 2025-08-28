using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Domain.Entities;

public class CharacterPosition
{
    public int Id { get; set; }

    public int CharacterId { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public DateTime ModDate { get; set; }

    [ForeignKey(nameof(CharacterId))]
    [InverseProperty(nameof(Character.CharacterPositions))]
    public virtual Character Character { get; set; }
}
