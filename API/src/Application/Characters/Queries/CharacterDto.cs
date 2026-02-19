namespace ProjectX.Application.Characters.Queries;
public class CharacterDto
{
    public required string Name { get; set; }

    public byte MainLevel { get; set; }

    public int Health { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterDto)} {{ Name = {Name}, MainLevel = {MainLevel}, Health = {Health} }}";
    }   
}
