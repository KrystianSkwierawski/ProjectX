namespace ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;

public class AddCharacterExperienceDto
{
    public byte Level { get; set; }

    public int Experience { get; set; }

    public override string ToString()
    {
        return $"{nameof(AddCharacterExperienceDto)} {{ Level = {Level}, Experience = {Experience} }}";
    }
}
