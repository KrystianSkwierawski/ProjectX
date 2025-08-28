namespace ProjectX.Application.PlayerPositions.Queries.GetPlayerPosition;
public class CharacterPositionDto
{
    public int CharacterId { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterPositionDto)} {{ CharacterId = {CharacterId}, X = {X}, Y = {Y}, Z = {Z} }}";
    }
}
