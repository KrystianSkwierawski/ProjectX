namespace ProjectX.Application.CharacterQuests.Commands.AbandonCharacterQuest;

public class AbandonCharacterQuestDto
{
    public int CharacterQuestId { get; set; }

    public bool Abandoned { get; set; }
}
