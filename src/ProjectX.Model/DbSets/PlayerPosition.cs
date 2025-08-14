using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Model.DbSets;

public class PlayerPosition
{
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public DateTime ModDate { get; set; }

    [ForeignKey(nameof(PlayerId))]
    [InverseProperty("PlayerPositions")]
    public virtual required Player Player { get; set; }
}
