namespace ProjectX.Application.PlayerPositions.Queries.GetPlayerPosition;
public class CharacterPositionDto
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterPositionDto)} {{ X = {X}, Y = {Y}, Z = {Z} }}";
    }
}
