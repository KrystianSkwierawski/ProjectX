namespace ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
public class AddCharacterExperienceDto
{
    public byte Level { get; set; }

    public byte SkillPoints { get; set; }

    public int Experience { get; set; }

    public bool LeveledUp { get; set; }

    public override string ToString()
    {
        return $"{nameof(AddCharacterExperienceDto)} {{ Level = {Level}, SkillPoints = {SkillPoints}, Experience = {Experience}, LeveledUp = {LeveledUp} }}";
    }
}
