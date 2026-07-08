using ProjectX.Domain.Enums;

namespace ProjectX.Application.Characters.Queries.GetCharacter;
public class CharacterDto
{
    public required string Name { get; set; }

    public required IDictionary<ExperienceTypeEnum, byte> Levels { get; set; }

    public int Health { get; set; }

    public int MaxHealth { get; set; }

    public short Strength { get; set; }

    public short Agility { get; set; }

    public short Stamina { get; set; }

    public short Intellect { get; set; }

    public short Spirit { get; set; }

    public short Armor { get; set; }

    public InventoryItemEnum Helmet { get; set; }

    public InventoryItemEnum Chest { get; set; }

    public InventoryItemEnum Boots { get; set; }

    public InventoryItemEnum Weapon { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterDto)} {{ Name = {Name}, Health = {Health}, MaxHealth = {MaxHealth}, Strength = {Strength}, Agility = {Agility}, Stamina = {Stamina}, Intellect = {Intellect}, Spirit = {Spirit}, Armor = {Armor}, Helmet = {Helmet}, Chest = {Chest}, Boots = {Boots}, Weapon = {Weapon} }}";
    }   
}
