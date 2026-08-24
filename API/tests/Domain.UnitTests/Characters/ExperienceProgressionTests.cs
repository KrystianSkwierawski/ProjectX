using ProjectX.Domain.Characters;

namespace ProjectX.Domain.UnitTests.Characters;

public class ExperienceProgressionTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(399, 2)]
    [InlineData(400, 3)]
    [InlineData(10000, 10)]
    [InlineData(int.MaxValue, 10)]
    public void GetLevel_ReturnsLevelForExperienceThreshold(int experience, byte expectedLevel)
    {
        Assert.Equal(expectedLevel, ExperienceProgression.GetLevel(experience));
    }
}
