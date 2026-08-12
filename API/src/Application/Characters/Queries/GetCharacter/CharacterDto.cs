using ProjectX.Domain.Enums;

namespace ProjectX.Application.Characters.Queries.GetCharacter;

public class CharacterDto
{
    public required string Name { get; set; }

    public required IDictionary<ExperienceTypeEnum, byte> Levels { get; set; }

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

    public override string ToString()
    {
        return $"{nameof(CharacterDto)} {{ Name = {Name}, Health = {Health}, MaxHealth = {MaxHealth}, Strength = {Strength}, Dexterity = {Dexterity}, Speed = {Speed}, Intellect = {Intellect}, Armor = {Armor}, HelmetType = {HelmetType}, ChestType = {ChestType}, BootsType = {BootsType}, WeaponType = {WeaponType}, AmmoType = {AmmoType}, AmmoCount = {AmmoCount} }}";
    }
}
