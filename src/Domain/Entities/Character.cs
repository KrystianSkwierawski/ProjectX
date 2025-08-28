using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Infrastructure.Identity;

namespace ProjectX.Domain.Entities;
public class Character
{
    public Character()
    {
        CharacterPositions = new HashSet<CharacterPosition>();
    }

    public int Id { get; set; }

    public string ApplicationUserId { get; set; }

    public DateTime ModDate { get; set; }

    [InverseProperty(nameof(CharacterPosition.Character))]
    public virtual ICollection<CharacterPosition> CharacterPositions { get; set; }

    [ForeignKey(nameof(ApplicationUserId))]
    [InverseProperty(nameof(ApplicationUser.Characters))]
    public virtual ApplicationUser ApplicationUser { get; set; }
}
