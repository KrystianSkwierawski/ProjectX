namespace ProjectX.Application.Characters.Queries.GetCharacters;

public class CharacterSummaryDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterSummaryDto)} {{ Id = {Id}, Name = {Name} }}";
    }
}
