using ProjectX.Domain.Enums;

namespace ProjectX.Application.Characters.Queries;
public class CharacterDto
{
    public required string Name { get; set; }

    public required IDictionary<ExperienceTypeEnum, byte> Levels { get; set; }

    public int Health { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterDto)} {{ Name = {Name}, Health = {Health} }}";
    }   
}
