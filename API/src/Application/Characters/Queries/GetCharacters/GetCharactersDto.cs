namespace ProjectX.Application.Characters.Queries.GetCharacters;

public class GetCharactersDto
{
    public required IList<CharacterSummaryDto> Characters { get; set; }

    public override string ToString()
    {
        return $"{nameof(GetCharactersDto)} {{ Count = {Characters.Count} }}";
    }
}
