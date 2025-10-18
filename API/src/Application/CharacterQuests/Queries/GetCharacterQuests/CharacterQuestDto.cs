using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;
public class CharacterQuestDto
{
    public int Id { get; set; }

    public int QuestId { get; set; }

    public CharacterQuestStatusEnum Status { get; set; }

    public int Progress { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterQuestDto)} {{ Id = {Id}, QuestId = {QuestId}, Status = {Status}, Progress = {Progress} }}";
    }
}
