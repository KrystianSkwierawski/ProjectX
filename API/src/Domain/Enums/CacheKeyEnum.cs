using System.Reflection;
using ProjectX.Application.Common.Attributes;

namespace ProjectX.Domain.Enums;
public enum CacheKeyEnum
{
    [CacheKeyParameters(
        Format = "common",
        ExpiryInSeconds = 3600
    )]
    Common,

    [CacheKeyParameters(
      Format = "quest_{0}",
      ExpiryInSeconds = 3600
    )]
    Quest,

    [CacheKeyParameters(
      Format = "quests",
      ExpiryInSeconds = 3600
    )]
    Quests,
}

public static class CacheKeyEnumExtensions
{
    public static CacheKeyParametersAttribute GetCacheKeyParametersAttribute(this CacheKeyEnum value)
    {
        var member = value
            .GetType()
            .GetMember(value.ToString())
            .First();

        return member.GetCustomAttribute<CacheKeyParametersAttribute>() ?? throw new ArgumentNullException(nameof(value));
    }
}
