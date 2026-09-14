namespace ProjectX.Application.CharacterQuests.Commands.CompleteCharacterQuest;

public class CompleteCharacterQuestDto
{
    public ProjectX.Domain.Enums.QuestEnum QuestId { get; set; }

    public CompleteCharacterQuestStatusEnum Status { get; set; }

    public int Reward { get; set; }

    public byte Level { get; set; }

    public override string ToString()
    {
        return $"{nameof(CompleteCharacterQuestDto)} {{ QuestId = {QuestId}, Status = {Status}, Reward = {Reward}, Level = {Level} }}";
    }
}

public enum CompleteCharacterQuestStatusEnum
{
    Applied,
    InventoryChanged
}
