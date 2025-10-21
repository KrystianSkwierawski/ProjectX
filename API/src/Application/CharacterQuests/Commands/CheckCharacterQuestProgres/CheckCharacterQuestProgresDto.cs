using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.CheckProgres;
public class CheckCharacterQuestProgresDto
{
    public int QuestId { get; set; }

    public int CharacterQuestId { get; set; }

    public CharacterQuestStatusEnum Status { get; set; }

    public override string ToString()
    {
        return $"{nameof(CheckCharacterQuestProgresDto)} {{ QuestId = {QuestId}, CharacterQuestId = {CharacterQuestId}, Status = {Status} }}";
    }
}

