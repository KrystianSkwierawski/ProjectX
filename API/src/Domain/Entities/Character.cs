using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Domain.Entities;
public class Character
{
    public Character()
    {
        CharacterPositions = new HashSet<CharacterTransform>();
        CharacterExperiences = new HashSet<CharacterExperience>();
    }

    public int Id { get; set; }

    public string ApplicationUserId { get; set; }

    public byte Level { get; set; } = 1;

    public byte SkillPoints { get; set; }

    public DateTime ModDate { get; set; }

    [InverseProperty(nameof(CharacterTransform.Character))]
    public virtual ICollection<CharacterTransform> CharacterPositions { get; set; }

    [InverseProperty(nameof(CharacterExperience.Character))]
    public virtual ICollection<CharacterExperience> CharacterExperiences { get; set; }

    [ForeignKey(nameof(ApplicationUserId))]
    [InverseProperty(nameof(ApplicationUser.Characters))]
    public virtual ApplicationUser ApplicationUser { get; set; }
}
