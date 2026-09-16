using ProjectX.Application.CharacterTransforms.Commands.SaveCharacterTransform;

namespace ProjectX.Application.UnitTests;

public class CharacterTransformValidationTests
{
    [Theory]
    [InlineData("DungeonScene")]
    [InlineData("UnknownScene")]
    [InlineData("")]
    public void Save_RejectsNonResumableScene(string scene)
    {
        var validator = new SaveCharacterTransformCommandValidator();

        var result = validator.Validate(new SaveCharacterTransformCommand { SceneName = scene });

        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SaveCharacterTransformCommand.SceneName));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void Save_RejectsNonFinitePosition(float position)
    {
        var validator = new SaveCharacterTransformCommandValidator();

        var result = validator.Validate(new SaveCharacterTransformCommand { PositionX = position });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Save_AcceptsWorldReturnPoint()
    {
        var validator = new SaveCharacterTransformCommandValidator();

        var result = validator.Validate(new SaveCharacterTransformCommand { PositionX = -5, PositionY = 1.15f, PositionZ = -6 });

        Assert.True(result.IsValid);
    }
}
