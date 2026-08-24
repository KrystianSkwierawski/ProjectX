namespace ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;

public class GetCharacterQuestsDto
{
    public required IList<CharacterQuestDto> CharacterQuests { get; set; }
}
