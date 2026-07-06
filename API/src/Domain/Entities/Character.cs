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

    public required string Name { get; set; }

    public int Health { get; set; }

    public int MaxHealth { get; set; }

    public short Strength { get; set; }

    public short Agility { get; set; }

    public short Stamina { get; set; }

    public short Intellect { get; set; }

    public short Spirit { get; set; }

    public short Arrmor { get; set; }

    public InventoryItemEnum Helmet { get; set; }

    public InventoryItemEnum Chest { get; set; }

    public InventoryItemEnum Boots { get; set; }

    public InventoryItemEnum Weapon { get; set; }

    public StatusEnum Status { get; set; }

    public DateTime ModDate { get; set; }

    public virtual CharacterInventory CharacterInventory { get; set; }

    public virtual ICollection<CharacterTransform> CharacterTransforms { get; set; }

    public virtual ICollection<CharacterExperience> CharacterExperiences { get; set; }

    public virtual ICollection<CharacterQuest> CharacterQuests { get; set; }

    public virtual ApplicationUser ApplicationUser { get; set; }
}
