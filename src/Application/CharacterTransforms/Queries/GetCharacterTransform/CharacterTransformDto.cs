namespace ProjectX.Application.CharacterTransforms.Queries.GetCharacterTransform;
public class CharacterTransformDto
{
    public int CharacterId { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public override string ToString()
    {
        return $"{nameof(CharacterTransformDto)} {{ CharacterId = {CharacterId}, X = {X}, Y = {Y}, Z = {Z} }}";
    }
}
