using ProjectX.Domain.Characters;
using ProjectX.Domain.Common;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class Character : BaseAuditableEntity
{
    public int Id { get; set; }

    public required string ApplicationUserId { get; set; }

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

    public virtual CharacterInventory CharacterInventory { get; set; } = null!;

    public virtual ICollection<CharacterTransform> CharacterTransforms { get; private set; } = new HashSet<CharacterTransform>();

    public virtual ICollection<CharacterExperience> CharacterExperiences { get; private set; } = new HashSet<CharacterExperience>();

    public virtual ICollection<CharacterQuest> CharacterQuests { get; private set; } = new HashSet<CharacterQuest>();

    public void AddTransform(CharacterTransform transform)
    {
        ArgumentNullException.ThrowIfNull(transform);

        CharacterTransforms.Add(transform);
    }

    public void UpdateState(CharacterStateUpdate update)
    {
        Health = update.Health ?? Health;
        MaxHealth = update.MaxHealth ?? MaxHealth;
        Strength = update.Strength ?? Strength;
        Dexterity = update.Dexterity ?? Dexterity;
        Speed = update.Speed ?? Speed;
        Intellect = update.Intellect ?? Intellect;
        Armor = update.Armor ?? Armor;
        HelmetType = update.HelmetType ?? HelmetType;
        ChestType = update.ChestType ?? ChestType;
        BootsType = update.BootsType ?? BootsType;
        WeaponType = update.WeaponType ?? WeaponType;
        AmmoType = update.AmmoType ?? AmmoType;
        AmmoCount = update.AmmoCount ?? AmmoCount;
    }
}
