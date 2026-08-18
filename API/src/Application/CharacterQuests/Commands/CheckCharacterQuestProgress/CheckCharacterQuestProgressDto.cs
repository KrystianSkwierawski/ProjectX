using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.CheckCharacterQuestProgress;

public class CheckCharacterQuestProgressDto
{
    public QuestEnum QuestId { get; set; }

    public int CharacterQuestId { get; set; }

    public int Progress { get; set; }

    public CharacterQuestStatusEnum Status { get; set; }

    public override string ToString()
    {
        return $"{nameof(CheckCharacterQuestProgressDto)} {{ QuestId = {QuestId}, CharacterQuestId = {CharacterQuestId}, Progress = {Progress}, Status = {Status} }}";
    }
}
