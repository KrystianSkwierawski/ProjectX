namespace ProjectX.Domain.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class QuestKillRequirementAttribute(string target, int count, string statusTextKey) : Attribute
{
    public string Target { get; } = target;

    public int Count { get; } = count;

    public string StatusTextKey { get; } = statusTextKey;
}
