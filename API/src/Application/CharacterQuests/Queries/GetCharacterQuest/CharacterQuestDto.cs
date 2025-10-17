using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Queries.GetCharacterQuest;
public class CharacterQuestDto
{
    public int Id { get; set; }

    public CharacterQuestStatusEnum Status { get; set; }

    public int Progress { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterQuestDto)} {{ Id = {Id}, Status = {Status}, Progress = {Progress} }}";
    }
}
