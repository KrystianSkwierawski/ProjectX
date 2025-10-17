namespace ProjectX.Application.Characters.Queries;
public class CharacterDto
{
    public required string Name { get; set; }

    public byte Level { get; set; }

    public byte SkillPoints { get; set; }

    public int Health { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterDto)} {{ Name = {Name}, Level = {Level}, SkillPoints = {SkillPoints}, Health = {Health} }}";
    }   
}
