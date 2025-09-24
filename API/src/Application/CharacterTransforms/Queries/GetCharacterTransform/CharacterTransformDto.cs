namespace ProjectX.Application.CharacterTransforms.Queries.GetCharacterTransform;
public class CharacterTransformDto
{
    public int CharacterId { get; set; }

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float PositionZ { get; set; }

    public float RotationY { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterTransformDto)} {{ CharacterId = {CharacterId}, PositionX = {PositionX}, PositionY = {PositionY}, PositionZ = {PositionZ}, RotationY = {RotationY} }}";
    }
}
