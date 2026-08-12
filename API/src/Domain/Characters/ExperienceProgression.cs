namespace ProjectX.Domain.Characters;

public static class ExperienceProgression
{
    private static readonly SortedDictionary<int, byte> ExperienceToLevel = new()
    {
        { 0, 1 },
        { 100, 2 },
        { 400, 3 },
        { 4000, 4 },
        { 5000, 5 },
        { 6000, 6 },
        { 7000, 7 },
        { 8000, 8 },
        { 9000, 9 },
        { 10000, 10 }
    };

    public static byte GetLevel(int experience)
    {
        return ExperienceToLevel
            .Where(level => level.Key <= experience)
            .Max(level => level.Value);
    }
}
