using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;
public class Character
{
    public Character()
    {
        CharacterTransforms = new HashSet<CharacterTransform>();
        CharacterExperiences = new HashSet<CharacterExperience>();
        CharacterQuests = new HashSet<CharacterQuest>();
    }

    public int Id { get; set; }

    public string ApplicationUserId { get; set; }

    public byte Level { get; set; } = 1;

    public byte SkillPoints { get; set; }

    public int Health { get; set; }

    public required string Name { get; set; }

    public StatusEnum Status { get; set; }

    public DateTime ModDate { get; set; }

    public virtual CharacterInventory CharacterInventory { get; set; }

    public virtual ICollection<CharacterTransform> CharacterTransforms { get; set; }

    public virtual ICollection<CharacterExperience> CharacterExperiences { get; set; }

    public virtual ICollection<CharacterQuest> CharacterQuests { get; set; }

    public virtual ApplicationUser ApplicationUser { get; set; }
}
