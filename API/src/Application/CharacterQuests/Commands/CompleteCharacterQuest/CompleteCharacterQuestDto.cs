namespace ProjectX.Application.CharacterQuests.Commands.CompleteCharacterQuest;

public class CompleteCharacterQuestDto
{
    public int Reward { get; set; }

    public override string ToString()
    {
        return $"{nameof(CompleteCharacterQuestDto)} {{ Reward = {Reward} }}";
    }
}
