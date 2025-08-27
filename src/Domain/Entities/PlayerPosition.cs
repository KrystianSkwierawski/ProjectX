using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Domain.Entities;

public class PlayerPosition
{
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public DateTime ModDate { get; set; }

    [ForeignKey(nameof(PlayerId))]
    [InverseProperty("PlayerPositions")]
    public virtual Player Player { get; set; }
}
