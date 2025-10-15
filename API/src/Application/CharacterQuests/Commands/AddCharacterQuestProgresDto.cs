using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands;
public class AddCharacterQuestProgresDto
{
    public CharacterQuestStatusEnum Status { get; set; }

    public int Reward { get; set; }

    public override string ToString()
    {
        return $"{nameof(AddCharacterQuestProgresDto)} {{ Status = {Status}, Reward = {Reward} }}";
    }
}
