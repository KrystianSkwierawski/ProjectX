namespace ProjectX.Application.PlayerPositions.Queries.GetPlayerPosition;
public class PlayerPositionDto
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public override string ToString()
    {
        return $"{nameof(PlayerPositionDto)} {{ X = {X}, Y = {Y}, Z = {Z} }}";
    }
}
