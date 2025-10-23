using System.ComponentModel.DataAnnotations.Schema;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;
public class Character
{
    public Character()
    {
        CharacterPositions = new HashSet<CharacterTransform>();
        CharacterExperiences = new HashSet<CharacterExperience>();
        CharacterQuests = new HashSet<CharacterQuest>();
    }

    public int Id { get; set; }

    public string ApplicationUserId { get; set; }

    public byte Level { get; set; } = 1;

    public byte SkillPoints { get; set; }

    public int Health { get; set; }

    public string Name { get; set; }

    public StatusEnum Status { get; set; }

    public DateTime ModDate { get; set; }

    public virtual CharacterInventory CharacterInventory { get; set; }

    [InverseProperty(nameof(CharacterTransform.Character))]
    public virtual ICollection<CharacterTransform> CharacterPositions { get; set; }

    [InverseProperty(nameof(CharacterExperience.Character))]
    public virtual ICollection<CharacterExperience> CharacterExperiences { get; set; }

    [InverseProperty(nameof(CharacterQuest.Character))]
    public virtual ICollection<CharacterQuest> CharacterQuests { get; set; }

    [ForeignKey(nameof(ApplicationUserId))]
    [InverseProperty(nameof(ApplicationUser.Characters))]
    public virtual ApplicationUser ApplicationUser { get; set; }
}
