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

    public short Dexterity { get; set; }

    public short Speed { get; set; }

    public short Intellect { get; set; }

    public short Armor { get; set; }

    public InventoryItemEnum HelmetType { get; set; }

    public InventoryItemEnum ChestType { get; set; }

    public InventoryItemEnum BootsType { get; set; }

    public InventoryItemEnum WeaponType { get; set; }

    public InventoryItemEnum AmmoType { get; set; }

    public int AmmoCount { get; set; }

    public StatusEnum Status { get; set; }

    public DateTime ModDate { get; set; }

    public virtual CharacterInventory CharacterInventory { get; set; }

    public virtual ICollection<CharacterTransform> CharacterTransforms { get; set; }

    public virtual ICollection<CharacterExperience> CharacterExperiences { get; set; }

    public virtual ICollection<CharacterQuest> CharacterQuests { get; set; }

    public virtual ApplicationUser ApplicationUser { get; set; }
}
