using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Domain.Entities;
public class Player
{
    public Player()
    {
        PlayerPositions = new HashSet<PlayerPosition>();
    }

    public int Id { get; set; }

    public DateTime ModDate { get; set; }

    [InverseProperty(nameof(Player))]
    public virtual ICollection<PlayerPosition> PlayerPositions { get; set; }
}
