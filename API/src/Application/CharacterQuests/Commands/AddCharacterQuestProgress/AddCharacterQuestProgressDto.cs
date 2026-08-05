using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.AddCharacterQuestProgress;
public class AddCharacterQuestProgressDto
{
    public CharacterQuestStatusEnum Status { get; set; }

    public int Reward { get; set; }

    public override string ToString()
    {
        return $"{nameof(AddCharacterQuestProgressDto)} {{ Status = {Status}, Reward = {Reward} }}";
    }
}
